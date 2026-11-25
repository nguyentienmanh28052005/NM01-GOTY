using System.IO;
using System.Linq;
using HanaMart.Actors;
using HanaMart.Player;
using HanaMart.Store;
using UnityEditor;
using UnityEngine;

namespace HanaMart.EditorTools
{
    /// Material-only preview. Runtime Actor animation is verified separately in Plan 3 Task 3.
    public static class CharacterPreview
    {
        static string prefix="characters";
        [MenuItem("HanaMart/QA/Character Materials")]
        public static void Run()
        {
            prefix="characters";
            if(!Application.isPlaying)throw new System.InvalidOperationException("Character preview runs only in Play mode");
            Application.runInBackground=true;
            var old=GameObject.Find("Character material preview");if(old)Object.DestroyImmediate(old);
            var root=new GameObject("Character material preview");
            var catalog=AssetDatabase.LoadAssetAtPath<ActorCatalog>(ActorSetup.CatalogPath);
            // Street directly in front of the original facade, retaining the store lighting.
            for(int i=0;i<4;i++)Create(root.transform,catalog.models[i],new Vector3(4.25f+i*1.18f,0,9.6f),false,i==2?.68f:1);
            Create(root.transform,catalog.models[1],new Vector3(9.1f,0,9.6f),true,1);
            var camera=Camera.main;var player=camera.GetComponent<PlayerController>();
            player.GameplayEnabled=false;player.Motor.frozen=true;player.Motor.vel=Vector2.zero;
            player.Motor.pos=new Vector2(6.6f,13.4f);player.Motor.eye=1.52f;player.Motor.SetYawFacing(0,-1);player.Motor.pitch=-.1f;
            player.Motor.Update(0,default,player.settings,player.Area);player.ApplyPose();
        }
        [MenuItem("HanaMart/QA/Character Masks")]
        public static void Masks()
        {
            if(!Application.isPlaying)throw new System.InvalidOperationException("Mask preview runs only in Play mode");
            Application.runInBackground=true;prefix="masks";
            var old=GameObject.Find("Character material preview");if(old)Object.DestroyImmediate(old);
            var root=new GameObject("Character material preview");var catalog=AssetDatabase.LoadAssetAtPath<ActorCatalog>(ActorSetup.CatalogPath);
            var kinds=new[]{"white","black","smile"};var entry=catalog.models[0];
            var material=entry.renderers.SelectMany(s=>s.materials).First(m=>m.shader.name=="HanaMart/Character");
            for(int i=0;i<3;i++)
            {
                var actor=Create(root.transform,entry,new Vector3(5+i,0,9.6f),false,1);
                ActorAccessories.Mask(actor.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Head),Quaternion.identity,1,material,kinds[i]);
            }
            var player=Camera.main.GetComponent<PlayerController>();player.GameplayEnabled=false;player.Motor.frozen=true;player.Motor.vel=Vector2.zero;
            player.Motor.pos=new Vector2(6,11.4f);player.Motor.eye=1.65f;player.Motor.SetYawFacing(0,-1);player.Motor.pitch=0;
            player.Motor.Update(0,default,player.settings,player.Area);player.ApplyPose();
        }
        static GameObject Create(Transform root,ActorCatalog.Entry entry,Vector3 threePosition,bool rain,float scale)
        {
            var actor=Object.Instantiate(entry.model,root);actor.name=entry.id+(rain?"_Rain":"");actor.transform.SetPositionAndRotation(ThreeCoords.ToUnity(threePosition),Quaternion.identity);
            var renderers=actor.GetComponentsInChildren<SkinnedMeshRenderer>();var bounds=renderers[0].bounds;
            foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            float factor=1.808f/Mathf.Max(.01f,bounds.size.y)*scale;actor.transform.localScale*=factor;
            actor.transform.position-=Vector3.up*(bounds.min.y-threePosition.y)*factor;
            foreach(var slot in entry.renderers)
            {
                var t=string.IsNullOrEmpty(slot.path)?actor.transform:actor.transform.Find(slot.path);
                var renderer=t.GetComponent<SkinnedMeshRenderer>();renderer.sharedMaterials=slot.materials;renderer.updateWhenOffscreen=true;
                var properties=new MaterialPropertyBlock();properties.SetFloat("_ActorScale",scale);renderer.SetPropertyBlock(properties);
                if(!rain)continue;
                if(!slot.rainMaterials.Any(m=>m && m.HasProperty("_Raincoat") && m.GetFloat("_Raincoat")>0))continue;
                var shell=new GameObject("Rain shell").AddComponent<SkinnedMeshRenderer>();shell.transform.SetParent(renderer.transform,false);shell.sharedMesh=renderer.sharedMesh;shell.bones=renderer.bones;shell.rootBone=renderer.rootBone;shell.sharedMaterials=slot.rainMaterials;shell.localBounds=renderer.localBounds;shell.updateWhenOffscreen=true;
            }
            if(rain)
            {
                var animator=actor.GetComponent<Animator>();var head=animator.GetBoneTransform(HumanBodyBones.Head);
                var material=entry.renderers.SelectMany(s=>s.rainMaterials).First(m=>m.HasProperty("_Raincoat"));
                ActorAccessories.Hood(head,Quaternion.identity,scale,material);
            }
            return actor;
        }
        [MenuItem("HanaMart/QA/Capture Character Materials")]
        public static void Capture()
        {
            var root=GameObject.Find("Character material preview");if(!root)throw new System.InvalidOperationException("Run Character Materials preview first");
            Directory.CreateDirectory("docs/visual/unity");
            var renderers=root.GetComponentsInChildren<Renderer>();
            bool asyncCompilation=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
            try{foreach(float reveal in new[]{0,.5f,1})
            {
                foreach(var renderer in renderers)
                {
                    var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);block.SetFloat("_Reveal",reveal);block.SetFloat("_EffectTime",2);renderer.SetPropertyBlock(block);
                }
                var camera=Camera.main;var target=RenderTexture.GetTemporary(1280,720,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);var keep=camera.targetTexture;camera.targetTexture=target;
                camera.Render();camera.Render();camera.targetTexture=keep;var previous=RenderTexture.active;RenderTexture.active=target;
                var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
                File.WriteAllBytes("docs/visual/unity/"+prefix+"-reveal-"+reveal.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture)+".png",image.EncodeToPNG());
                RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);Object.DestroyImmediate(image);
            }}finally{ShaderUtil.allowAsyncCompilation=asyncCompilation;}
        }

        [MenuItem("HanaMart/QA/Capture Actor Motion")]
        public static void CaptureActorMotion()
        {
            if (!Application.isPlaying) throw new System.InvalidOperationException("Actor preview runs only in Play mode");
            Application.runInBackground = true;
            var camera = Camera.main;
            var player = camera.GetComponent<PlayerController>();
            player.StartCoroutine(CaptureMotionFrames(camera, player));
        }

        static System.Collections.IEnumerator CaptureMotionFrames(Camera camera, PlayerController player)
        {
            player.GameplayEnabled = false;
            player.Motor.frozen = true;
            player.Motor.vel = Vector2.zero;
            player.Motor.pos = new Vector2(6.6f, 13.8f);
            player.Motor.eye = 1.52f;
            player.Motor.SetYawFacing(0, -1);
            player.Motor.pitch = -.1f;
            player.Motor.Update(0, default, player.settings, player.Area);
            player.ApplyPose();

            var previous = GameObject.Find("Actor motion preview");
            if (previous) Object.DestroyImmediate(previous);
            var staging = new GameObject("Actor motion preview");
            var catalog = AssetDatabase.LoadAssetAtPath<ActorCatalog>(ActorSetup.CatalogPath);
            var actors = new Actor[5];
            var colors = new[] { 0x055a63, 0x8b3e46, 0x364a52, 0xf2c200, 0x4b5358 };
            for (int i = 0; i < actors.Length; ++i)
            {
                var options = new ActorOptions
                {
                    modelIndex = i == 3 ? 1 : i == 4 ? 3 : i,
                    coat = i == 3 ? "raincoat" : i == 2 ? "child" : "padding",
                    color = colors[i],
                    hood = i == 3,
                    face = i == 3 ? "wide" : "normal",
                };
                var actor = Actor.Spawn(catalog, options, new Vector3(4.25f + i * 1.17f, .012f, 10.45f));
                actor.transform.SetParent(staging.transform, true);
                actor.lookTarget = ThreeCoords.ToUnity(camera.transform.position);
                actor.AutoTick = false;
                actors[i] = actor;
            }
            Directory.CreateDirectory("docs/visual/unity");
            yield return null;
            CaptureFrame(camera, "docs/visual/unity/actors-idle.png");
            for (int frame = 0; frame < 20; ++frame)
                for (int i = 0; i < actors.Length; ++i)
                    actors[i].StepPose(1f / 60, 1.15f);
            yield return null;
            CaptureFrame(camera, "docs/visual/unity/actors-walk.png");
            actors[3].Motion.revealTarget = 1;
            for (int frame = 0; frame < 120; ++frame)
                for (int i = 0; i < actors.Length; ++i)
                    actors[i].StepPose(1f / 60, 0);
            yield return null;
            CaptureFrame(camera, "docs/visual/unity/actors-reveal.png");
        }

        internal static void CaptureFrame(Camera camera, string output)
        {
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            bool previousAsync = ShaderUtil.allowAsyncCompilation;
            var target = RenderTexture.GetTemporary(1280, 720, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                ShaderUtil.allowAsyncCompilation = false;
                camera.targetTexture = target;
                camera.Render();
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                image.Apply();
                File.WriteAllBytes(output, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                ShaderUtil.allowAsyncCompilation = previousAsync;
                RenderTexture.ReleaseTemporary(target);
                Object.DestroyImmediate(image);
            }
        }
    }
}
