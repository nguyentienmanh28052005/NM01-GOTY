using System;
using System.Collections.Generic;
using HanaMart.Store;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;

namespace HanaMart.Actors
{
    public sealed class Actor : MonoBehaviour
    {
        public Vector3 pos;
        public float yaw, speed = 1.15f;
        public bool alive = true, moving;
        public bool AutoTick { get; set; } = true;
        public Vector3? lookTarget;
        public ActorMotion Motion { get; } = new ActorMotion();
        public GameObject Model { get; private set; }
        public Animator Animator { get; private set; }
        public ActorOptions Options { get; private set; }

        readonly List<Renderer> characterSurfaces = new();
        readonly List<Transform> eyes = new();
        readonly List<UnityEngine.Object> resources = new();
        MaterialPropertyBlock appearance;
        MaterialPropertyBlock eyeEffect;
        static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        static readonly int Reveal = Shader.PropertyToID("_Reveal");
        static readonly int EffectTime = Shader.PropertyToID("_EffectTime");
        PlayableGraph graph;
        AnimationClipPlayable walk;
        float secondsPerLoop;
        Transform[] bones;
        LocalPose[] idlePose;
        Transform head, neck, hips, chest;
        Transform leftShoulder, leftElbow, rightElbow, leftHand, rightHand;
        SkinnedMeshRenderer shoes;
        Mesh bakedShoes;
        readonly List<Vector3> shoeVertices = new();
        float idleHeightCorrection;
        Vector3 modelPosition;
        Quaternion modelRotation;
        IReadOnlyList<Vector2> path;
        int waypoint;

        public static Actor Spawn(ActorCatalog catalog, ActorOptions options, Vector3 position)
        {
            if (!catalog || !catalog.walk) throw new ArgumentException("Actor catalog needs a humanoid walk clip.", nameof(catalog));
            options ??= new ActorOptions();
            var setup = catalog.models[options.ModelIndex];
            var root = new GameObject(setup.id + " (" + options.coat + ")");
            var actor = root.AddComponent<Actor>();
            try
            {
                actor.Initialize(catalog.walk, setup, options, position);
                return actor;
            }
            catch
            {
                Release(root);
                throw;
            }
        }

        void Initialize(AnimationClip clip, ActorCatalog.Entry setup, ActorOptions options, Vector3 position)
        {
            appearance = new MaterialPropertyBlock();
            eyeEffect = new MaterialPropertyBlock();
            Options = options;
            pos = position;
            transform.position = ThreeCoords.ToUnity(pos);
            Model = Instantiate(setup.model, transform);
            Animator = Model.GetComponent<Animator>();
            Animator.avatar = setup.avatar;
            Animator.runtimeAnimatorController = null;
            Animator.applyRootMotion = false;
            Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            Animator.Rebind();

            var modelRenderers = Model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var bounds = modelRenderers[0].bounds;
            foreach (var renderer in modelRenderers) bounds.Encapsulate(renderer.bounds);
            var dimensions = options.Scale;
            float unit = 1.808f / Mathf.Max(.01f, bounds.size.y);
            Model.transform.localScale = Vector3.Scale(Model.transform.localScale, dimensions * unit);
            Model.transform.localPosition -= Vector3.up * (bounds.min.y - transform.position.y) * unit * dimensions.y;
            modelPosition = Model.transform.localPosition;
            modelRotation = Model.transform.localRotation;

            head = Animator.GetBoneTransform(HumanBodyBones.Head);
            neck = Animator.GetBoneTransform(HumanBodyBones.Neck);
            hips = Animator.GetBoneTransform(HumanBodyBones.Hips);
            chest = Animator.GetBoneTransform(HumanBodyBones.Chest);
            leftShoulder = Animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            leftElbow = Animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            rightElbow = Animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            leftHand = Animator.GetBoneTransform(HumanBodyBones.LeftHand);
            rightHand = Animator.GetBoneTransform(HumanBodyBones.RightHand);
            LowerArm(leftShoulder, leftElbow, leftHand, -1);
            LowerArm(Animator.GetBoneTransform(HumanBodyBones.RightUpperArm), rightElbow, rightHand, 1);

            bones = Array.FindAll(Model.GetComponentsInChildren<Transform>(true), bone => bone != Model.transform);
            idlePose = Array.ConvertAll(bones, bone => new LocalPose(bone));
            BindMaterials(setup);
            MakeEyes();

            secondsPerLoop = clip.length;
            graph = PlayableGraph.Create(name);
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            walk = AnimationClipPlayable.Create(graph, clip);
            walk.SetApplyFootIK(false);
            var output = AnimationPlayableOutput.Create(graph, "Walk", Animator);
            output.SetSourcePlayable(walk);
            graph.Play();
            BindShoes(modelRenderers);
            StepPose(0, 0);
        }

        void LowerArm(Transform shoulder, Transform elbow, Transform hand, float side)
        {
            if (!shoulder || !elbow || !hand) return;
            var upper = transform.TransformDirection(new Vector3(side * .13f, -1, .02f));
            var lower = transform.TransformDirection(new Vector3(side * .08f, -1, .19f));
            shoulder.rotation = Quaternion.FromToRotation(elbow.position - shoulder.position, upper) * shoulder.rotation;
            elbow.rotation = Quaternion.FromToRotation(hand.position - elbow.position, lower) * elbow.rotation;
        }

        void BindShoes(SkinnedMeshRenderer[] renderers)
        {
            shoes = Array.Find(renderers, renderer =>
                renderer.name.Contains("Shoes", StringComparison.OrdinalIgnoreCase) ||
                renderer.name.Contains("Sneakers", StringComparison.OrdinalIgnoreCase));
            if (!shoes) return;
            bakedShoes = new Mesh { name = "Actor floor contact" };
            bakedShoes.MarkDynamic();
            resources.Add(bakedShoes);
            idleHeightCorrection = FloorCorrection();
        }

        float FloorCorrection()
        {
            // Retargeted ankle and toon bone positions alone don't describe the shoe soles.
            // Bake only the shoe renderer and reuse both the mesh and vertex list.
            shoes.BakeMesh(bakedShoes);
            bakedShoes.GetVertices(shoeVertices);
            var matrix = shoes.localToWorldMatrix;
            float minimum = float.PositiveInfinity;
            foreach (var vertex in shoeVertices)
                minimum = Mathf.Min(minimum, matrix.MultiplyPoint3x4(vertex).y);
            return float.IsInfinity(minimum) ? 0 : transform.position.y - minimum;
        }

        void BindMaterials(ActorCatalog.Entry setup)
        {
            bool rain = Options.coat == "raincoat";
            Material bodyMaterial = null;
            Material shellMaterial = null;
            foreach (var slot in setup.renderers)
            {
                var target = string.IsNullOrEmpty(slot.path) ? Model.transform : Model.transform.Find(slot.path);
                if (!target || !target.TryGetComponent<SkinnedMeshRenderer>(out var renderer)) continue;
                renderer.sharedMaterials = slot.materials;
                renderer.updateWhenOffscreen = true;
                if (Options.noShadow) renderer.shadowCastingMode = ShadowCastingMode.Off;
                characterSurfaces.Add(renderer);
                foreach (var material in slot.materials)
                    if (material && material.HasProperty(Reveal)) bodyMaterial ??= material;

                if (!rain || !Array.Exists(slot.rainMaterials, material => material && material.HasProperty("_Raincoat"))) continue;
                var shell = new GameObject("Rain shell").AddComponent<SkinnedMeshRenderer>();
                shell.transform.SetParent(renderer.transform, false);
                shell.sharedMesh = renderer.sharedMesh;
                shell.bones = renderer.bones;
                shell.rootBone = renderer.rootBone;
                shell.sharedMaterials = slot.rainMaterials;
                shell.localBounds = renderer.localBounds;
                shell.updateWhenOffscreen = true;
                if (Options.noShadow) shell.shadowCastingMode = ShadowCastingMode.Off;
                characterSurfaces.Add(shell);
                foreach (var material in slot.rainMaterials)
                    if (material && material.HasProperty("_Raincoat")) shellMaterial ??= material;
            }
            if (!head || !bodyMaterial) return;
            if (Options.hood)
                RegisterAccessory(ActorAccessories.Hood(head, transform.rotation, Options.Scale.x, shellMaterial ? shellMaterial : bodyMaterial));
            if (!string.IsNullOrEmpty(Options.mask))
                RegisterAccessory(ActorAccessories.Mask(head, transform.rotation, Options.Scale.x, bodyMaterial, Options.mask));
        }

        void RegisterAccessory(GameObject accessory)
        {
            if (!accessory) return;
            var renderer = accessory.GetComponent<Renderer>();
            if (Options.noShadow) renderer.shadowCastingMode = ShadowCastingMode.Off;
            characterSurfaces.Add(renderer);
        }

        void MakeEyes()
        {
            if (!head || Options.face == "none") return;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) return;
            var material = new Material(shader) { color = new Color(.12f, .02f, .02f) };
            material.EnableKeyword("_EMISSION");
            resources.Add(material);
            foreach (int side in new[] { -1, 1 })
            {
                var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                eye.name = "Reveal eye";
                var collider = eye.GetComponent<Collider>();
                collider.enabled = false;
                Release(collider);
                eye.transform.SetPositionAndRotation(
                    head.position + transform.TransformDirection(new Vector3(side * .036f, .063f, .121f) * Options.Scale.x),
                    transform.rotation);
                eye.transform.SetParent(head, true);
                eye.GetComponent<Renderer>().sharedMaterial = material;
                eye.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                eyes.Add(eye.transform);
            }
        }

        public void SetPath(IReadOnlyList<Vector2> points)
        {
            path = points;
            waypoint = 0;
        }

        public void Advance(float dt)
        {
            if (!alive || !Model) return;
            moving = false;
            if (path != null && dt > 0)
            {
                for (int i = waypoint; i < path.Count; ++i)
                {
                    Vector2 direction = path[i] - new Vector2(pos.x, pos.z);
                    float distance = direction.magnitude;
                    if (distance < .06f)
                    {
                        waypoint++;
                        continue;
                    }
                    float distanceThisFrame = Mathf.Min(distance, speed * dt);
                    pos += new Vector3(direction.x, 0, direction.y) * (distanceThisFrame / distance);
                    float target = Mathf.Atan2(direction.x, direction.y);
                    yaw += Mathf.DeltaAngle(yaw * Mathf.Rad2Deg, target * Mathf.Rad2Deg) * Mathf.Deg2Rad * Mathf.Min(1, dt * 7);
                    moving = distanceThisFrame > 0;
                    break;
                }
            }
            StepPose(dt, moving ? speed : 0);
        }

        void LateUpdate()
        {
            if (AutoTick) Advance(Time.deltaTime);
        }

        public void StepPose(float dt, float movementSpeed)
        {
            if (!alive || !Model) return;
            transform.SetPositionAndRotation(ThreeCoords.ToUnity(pos), Quaternion.Euler(0, -yaw * Mathf.Rad2Deg, 0));
            if (lookTarget.HasValue) Motion.LookAt(lookTarget.Value, pos, yaw, dt, Options);
            else if (dt > 0)
            {
                Motion.headYaw *= .9f;
                Motion.headPitch *= .9f;
            }
            Motion.Tick(dt, movementSpeed, Options);

            for (int i = 0; i < bones.Length; ++i) idlePose[i].Apply(bones[i]);
            if (Motion.blend > .001f)
            {
                double phase = Mathf.Repeat(Motion.phase / (Mathf.PI * 2), 1);
                walk.SetTime(phase * secondsPerLoop);
                graph.Evaluate(0);
                for (int i = 0; i < bones.Length; ++i) idlePose[i].BlendAnimated(bones[i], Motion.blend);
            }
            Model.transform.SetLocalPositionAndRotation(modelPosition, modelRotation);
            ApplyBoneEffects();
            if (shoes) Model.transform.position += Vector3.up * (Motion.blend > .001f ? FloorCorrection() : idleHeightCorrection);
            ApplyAppearance();
        }

        void ApplyBoneEffects()
        {
            var pose = Motion.Pose;
            var scale = Options.Scale;
            if (hips)
            {
                hips.position += transform.TransformDirection(new Vector3(-pose.hipX * scale.x, (pose.hipY - .9f) * scale.y, 0));
                RotateBone(hips, 0, 0, -pose.hipRoll);
            }
            if (chest)
            {
                RotateBone(chest, pose.torsoPitch, -pose.torsoYaw, 0);
                chest.localScale = Vector3.Scale(chest.localScale, new Vector3(pose.bodyScaleX, 1, pose.bodyScaleZ / .72f));
            }
            if (leftShoulder) RotateBone(leftShoulder, -Motion.armUp * 1.1f, 0, 0);
            if (leftElbow)
            {
                leftElbow.localPosition *= pose.armScale;
                RotateBone(leftElbow, -Motion.armUp * .4f, 0, 0);
            }
            if (rightElbow) rightElbow.localPosition *= pose.armScale;
            if (leftHand) leftHand.localPosition *= pose.armScale;
            if (rightHand) rightHand.localPosition *= pose.armScale;
            if (head)
            {
                Vector3 destination = head.position + transform.up * (pose.headRise * scale.y);
                if (neck && neck != head) neck.position += transform.up * (.16f * Motion.reveal * scale.y);
                head.position = destination;
                RotateBone(head, pose.headPitch, -pose.headYaw, -pose.headRoll);
            }
        }

        void RotateBone(Transform bone, float pitch, float angle, float roll)
        {
            Quaternion delta = Quaternion.Euler(new Vector3(pitch, angle, roll) * Mathf.Rad2Deg);
            Quaternion facing = transform.rotation;
            bone.rotation = facing * delta * Quaternion.Inverse(facing) * bone.rotation;
        }

        void ApplyAppearance()
        {
            appearance.SetColor("_CoatColor", Hex(Options.color));
            appearance.SetColor("_PantsColor", Hex(Options.pants));
            appearance.SetColor("_SkinColor", Hex(Options.face == "none" ? 0xd8d4cc : Options.skin));
            appearance.SetFloat("_ActorScale", Options.Scale.x);
            appearance.SetFloat("_TintStrength", 1);
            appearance.SetFloat("_Faceless", Options.face == "none" ? 1 : 0);
            appearance.SetFloat("_Pale", Options.pale ? 1 : 0);
            appearance.SetFloat(Reveal, Motion.reveal);
            appearance.SetFloat(EffectTime, Motion.time);
            foreach (var surface in characterSurfaces) surface.SetPropertyBlock(appearance);

            eyeEffect.SetColor(EmissionColor, Hex(0xff1a10) * Motion.Pose.eyeEmission);
            foreach (var eye in eyes)
            {
                eye.gameObject.SetActive(Motion.reveal > .05f);
                eye.localScale = Vector3.one * (.016f * Motion.Pose.eyeScale * Options.Scale.x / Mathf.Abs(head.lossyScale.x));
                eye.GetComponent<Renderer>().SetPropertyBlock(eyeEffect);
            }
        }

        static Color Hex(int rgb) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);

        public void Remove()
        {
            if (!alive) return;
            alive = false;
            Release(gameObject);
        }

        void OnDestroy()
        {
            if (graph.IsValid()) graph.Destroy();
            foreach (var resource in resources) Release(resource);
        }

        static void Release(UnityEngine.Object value)
        {
            if (!value) return;
            if (Application.isPlaying) Destroy(value);
            else DestroyImmediate(value);
        }

        readonly struct LocalPose
        {
            readonly Vector3 position, scale;
            readonly Quaternion rotation;

            public LocalPose(Transform bone)
            {
                position = bone.localPosition;
                scale = bone.localScale;
                rotation = bone.localRotation;
            }

            public void Apply(Transform bone)
            {
                bone.SetLocalPositionAndRotation(position, rotation);
                bone.localScale = scale;
            }

            public void BlendAnimated(Transform bone, float amount)
            {
                bone.SetLocalPositionAndRotation(
                    Vector3.Lerp(position, bone.localPosition, amount),
                    Quaternion.Slerp(rotation, bone.localRotation, amount));
                bone.localScale = Vector3.Lerp(scale, bone.localScale, amount);
            }
        }

    }
}
