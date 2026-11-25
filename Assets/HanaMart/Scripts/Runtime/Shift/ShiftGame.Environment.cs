using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HanaMart.Actors;
using HanaMart.Interaction;
using HanaMart.Store;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HanaMart.Shift
{
    public sealed partial class ShiftGame
    {
        sealed class ShopLight
        {
            public Light Lamp;
            public float Brightness, FlickerUntil;
            public Color Color;
            public bool Inside, Alley, Off;
            public readonly List<(Renderer renderer, Color baseColor, Color emissionColor)> Panels = new();
        }

        [Serializable]
        sealed class HitLabel { public string id, label; }

        readonly HashSet<GameObject> dynamics = new();
        readonly Dictionary<StoreMarker, GameObject> looseStock = new();
        readonly List<GameObject> prints = new();
        readonly List<ShopLight> lamps = new();
        MaterialPropertyBlock tintBlock;
        StoreMarker printHit, spillHit;
        GameObject spill;
        Renderer monitor;
        Camera securityCamera;
        RenderTexture securityImage;
        Actor cctvDouble, cctvGhost;
        float cameraFrame, cctvGhostSeconds;
        int cameraTick, printGeneration;
        float ghostDoorFraction = .35f;
        float baseAmbient, baseReflection;
        bool insidePower = true, outsidePower = true;
        HemisphereAmbient hemisphere;
        float litAmbient = 1, ambientLevel = 1;
        Color litTint = Color.white, ambientTint = Color.white;

        void BindEnvironment(StoreMarker[] sceneObjects)
        {
            tintBlock = new MaterialPropertyBlock();
            baseAmbient = RenderSettings.ambientIntensity;
            baseReflection = RenderSettings.reflectionIntensity;
            hemisphere = FindFirstObjectByType<HemisphereAmbient>();
            foreach (var marker in sceneObjects)
            {
                if (marker.kind != StoreMarker.Kind.Light) continue;
                var lamp = marker.GetComponentInChildren<Light>(true);
                if (!lamp) continue;
                var p = ThreeCoords.ToUnity(lamp.transform.position);
                bool alley = marker.key.Contains("alley", StringComparison.OrdinalIgnoreCase);
                bool spillLight = marker.key.Contains("spill", StringComparison.OrdinalIgnoreCase);
                lamps.Add(new ShopLight
                {
                    Lamp = lamp, Brightness = lamp.intensity, Color = lamp.color,
                    Inside = spillLight || (p.x >= 0 && p.x <= 12 && p.z < 8 && p.z >= -4.6f),
                    Alley = alley,
                });
            }
            var ceilings = lamps.Where(light => light.Inside && light.Lamp.transform.position.y > 2.5f).ToArray();
            var sceneRenderers = doors ? doors.GetComponentsInChildren<Renderer>(true)
                : FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var renderer in sceneRenderers)
            {
                if (renderer.transform.position.y <= 2.94f || renderer.transform.position.y >= 2.97f) continue;
                var mat = renderer.sharedMaterial;
                if (!mat || !mat.HasProperty("_EmissionColor") || mat.GetColor("_EmissionColor").maxColorComponent <= .1f) continue;
                var owner = ceilings.OrderBy(light => Vector2.Distance(
                    new Vector2(light.Lamp.transform.position.x, light.Lamp.transform.position.z),
                    new Vector2(renderer.transform.position.x, renderer.transform.position.z))).FirstOrDefault();
                if (owner != null)
                {
                    Color baseColor = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : Color.white;
                    owner.Panels.Add((renderer, baseColor, mat.GetColor("_EmissionColor")));
                }
            }
            var screen = Marker("R:monScreen");
            monitor = screen ? screen.GetComponent<Renderer>() : null;
        }

        void ResetEnvironment()
        {
            foreach (var prop in dynamics) if (prop) Destroy(prop);
            dynamics.Clear();
            looseStock.Clear();
            prints.Clear();
            ++printGeneration;
            printHit = spillHit = null;
            spill = null;
            ghostDoorFraction = .35f;
            cctvGhostSeconds = cameraFrame = 0;
            cameraTick = 0;
            if (cctvDouble) cctvDouble.Remove();
            if (cctvGhost) cctvGhost.Remove();
            cctvDouble = cctvGhost = null;
            litAmbient = 1;
            litTint = Color.white;
            foreach (var light in lamps)
            {
                if (!light.Lamp) continue;
                light.FlickerUntil = 0;
                light.Off = false;
                light.Lamp.color = light.Color;
            }
            Power(true, true);
            TintStore(0xffffff, 1);
            if (monitor)
            {
                tintBlock ??= new MaterialPropertyBlock();
                tintBlock.Clear();
                tintBlock.SetColor("_BaseColor", Color.white);
                tintBlock.SetColor("_EmissionColor", Color.white);
                if (securityImage)
                {
                    tintBlock.SetTexture("_BaseMap", securityImage);
                    tintBlock.SetTexture("_EmissionMap", securityImage);
                }
                monitor.SetPropertyBlock(tintBlock);
            }
        }

        void ReleaseEnvironment()
        {
            ResetEnvironment();
            if (securityCamera) Destroy(securityCamera.gameObject);
            if (securityImage) { securityImage.Release(); Destroy(securityImage); }
            ReleaseEffects();
            if (cardboard) Destroy(cardboard);
        }

        void UpdateEnvironment(float dt)
        {
            foreach (var light in lamps)
            {
                if (!light.Lamp) continue;
                float variance = light.FlickerUntil > Time.time && Mathf.Sin(Time.time * 47 + light.Lamp.GetInstanceID()) < .35f ? .19f : 1;
                bool powered = light.Inside ? insidePower : outsidePower;
                ApplyLamp(light, light.Off || !powered ? 0 : variance);
            }
            if (HasFlag("cctvGhost"))
            {
                cctvGhostSeconds -= dt;
                if (cctvGhostSeconds <= 0) Run.Progress.Flags.Remove("cctvGhost");
            }
            cameraFrame -= dt;
            if (cameraFrame > 0 || !monitor) return;
            cameraFrame = .35f;
            bool doubled = HasFlag("double"), ghost = HasFlag("cctvGhost");
            if (!doubled && !ghost && Vector2.Distance(player.Motor.pos, new Vector2(.3f, 2.6f)) > 4.5f) return;
            RenderSecurityFeed(doubled, ghost);
        }

        void Ambient(string kind)
        {
            if (HasFlag("red") || HasFlag("blackout")) return;
            switch (kind)
            {
                case "still": SpawnStill(); break;
                case "whisper": WhisperClue(); break;
                case "doorHeld":
                    ghostDoorTime = 999;
                    ghostDoorFraction = 1;
                    PlayClip("door");
                    Say("(Cửa tự động bật mở. Không có ai. Chuông KHÔNG reo. Mưa hắt vào sàn.)");
                    AddTask("door", "Cửa tự động bị kẹt mở — ra cửa đóng lại");
                    break;
                case "cctvGhost":
                    cctvGhostSeconds = 40;
                    Run.Progress.Flags["cctvGhost"] = 1;
                    AddTask("aisle", "Camera quay được một người ở cuối lối giữa — ra kiểm tra");
                    break;
                case "handprints": Handprints(); break;
                case "fridge":
                    if (doors && doors.Fridges.Count > 0) doors.Fridges[RandomIndex(doors.Fridges.Count)].motor.target = .75f;
                    PlayClip("creak");
                    Say("(Một cánh tủ mát tự bật mở. Hơi lạnh tràn ra sàn.)", 3);
                    AddTask("fridgeClose", "Đóng cánh tủ mát bị mở (để lâu hàng sẽ hỏng)");
                    fridgeOpenTime = 0;
                    break;
                case "radio": RadioClue(); break;
                case "steps":
                    foreach (float second in new[] { 0, .5f, 1, 1.5f, 2.2f }) Later(second, () => PlayAt("footstep_walk", new Vector3(2.6f, 0, -2.5f)));
                    Say("(Tiếng bước chân trong kho. Không ai được vào kho...)");
                    AddTask("storage", "Có tiếng bước chân trong kho — vào kiểm tra");
                    break;
                case "fakePhone":
                    PlayClip("ring");
                    PhoneLed(0x40ff60);
                    Later(1.2f, () => { if (!phone.Ringing) PhoneLed(0x103010); });
                    Say("(Điện thoại bàn reo đúng MỘT tiếng rồi im bặt.)", 3);
                    Cinematics.Pulse = .4f;
                    break;
                case "lightDie": Flicker(true); break;
                case "flicker": Flicker(false); break;
                case "backKnock":
                    var position = new Vector3(2.1f, 1.4f, -4.7f);
                    PlayAt("knock", position);
                    Later(.5f, () => PlayAt("knock", position));
                    Say("(Ai đó gõ cửa sau hai tiếng. Hàng giao chưa tới giờ.)", 3);
                    break;
                case "can": DropShelfItem(); break;
                case "spill": MakeSpill(); break;
            }
        }

        void RadioClue()
        {
            double minute = Run.Timeline.NextAnchor(Run.Clock.Minutes, 3)?.Minute ?? 999;
            string line = minute < 125 ? "...hai giờ... tủ đông... đừng... mở..." : minute < 185 ? "...ba giờ... mười hai... không phải mười ba..." :
                minute < 230 ? "...người mặc vest... kho... không..." : minute < 250 ? "...tối... ngồi xuống... đừng nhìn ra..." :
                minute < 290 ? "...bốn giờ bốn mươi bốn... reng... reng..." : minute < 320 ? "...đèn đỏ... đứng yên... đứng yên..." : "...chị ấy... chưa tới đâu... sáu giờ...";
            PlayClip("radio_glitch");
            Say($"Loa cửa hàng (rè): \"{line}\"", 5);
        }

        void WhisperClue()
        {
            var spots = new Dictionary<string, string>
            {
                { "drawer", "ngăn kéo dưới máy tính tiền" }, { "locker", "tủ đồ trong kho" }, { "micro2", "lò vi sóng thứ hai" },
                { "dumpster", "nắp thùng rác ngoài hẻm" }, { "rulesBack", "mặt sau tờ nội quy... đọc lại nó" },
            };
            var left = spots.Where(spot => !ReadDiary(spot.Key)).Select(spot => spot.Value).ToArray();
            PlayAt("whisper", ThreeCoords.ToUnity(transform.position - transform.forward * .7f));
            Say(left.Length > 0 ? "(thì thầm sau gáy) ...tớ để lại giấy... ở " + left[RandomIndex(left.Length)] + "..." :
                "(thì thầm) ...cảm ơn cậu đã đọc...", 5);
        }

        void Flicker(bool stayOff)
        {
            var inside = lamps.Where(light => light.Inside).ToArray();
            if (inside.Length == 0) return;
            var light = stayOff ? inside[RandomIndex(inside.Length)] : inside[Mathf.Min(3, inside.Length - 1)];
            light.FlickerUntil = Time.time + (stayOff ? 1.2f : 9);
            PlayAt("buzz", ThreeCoords.ToUnity(light.Lamp.transform.position));
            if (stayOff)
            {
                Later(1.2f, () => light.Off = true);
                Later(14 + RandomValue() * 8, () => light.Off = false);
                Say("(Một bóng đèn rè lên rồi tắt hẳn. Góc đó tối om.)", 3);
            }
            else Say(Run.Clock.Minutes < 226 ? "(Đèn chập, tóe lửa. Điện đêm nay chập chờn... có khi sắp mất điện.)" : "(Đèn chập, tóe lửa.)");
        }

        void SpawnPasserby()
        {
            var profile = new Profile("Bóng áo mưa ngoài kính", new ActorOptions
                { coat = "raincoat", color = 0xf2c200, hood = true, face = "none", pale = true, uncanny = 1 }, -1, null, null, .75f);
            var passer = SpawnGuest("passerby", profile, new Vector3(13.5f, 0, 9), false);
            StartCoroutine(PassOutside(passer));
        }

        IEnumerator PassOutside(Guest passer)
        {
            yield return Walk(passer, new Vector2(7, 9));
            yield return Face(passer.Actor, new Vector2(7, 0));
            passer.Actor.lookTarget = ThreeCoords.ToUnity(transform.position);
            yield return new WaitForSeconds(5);
            passer.Actor.lookTarget = null;
            yield return Walk(passer, new Vector2(-4, 9));
            RemoveGuest(passer);
        }

        void DropShelfItem()
        {
            float side = RandomValue() < .5f ? -1 : 1;
            var position = new Vector3(7.2f + side * .42f, 1.18f, 2.5f + RandomValue() * 2.5f);
            var item = CreateProp("can", position, .095f);
            if (!item) return;
            dynamics.Add(item);
            var hit = NewHit("fallen", "Hàng rơi", new Vector3(position.x + side * .4f, .15f, position.z), new Vector3(.4f, .3f, .4f));
            looseStock.Add(hit, item);
            var physics = item.AddComponent<ItemPhysicsBody>();
            physics.Body = new ItemBody
            {
                position = position, velocity = new Vector3(side * (.9f + RandomValue() * .6f), .4f, (RandomValue() - .5f) * .6f),
                spin = new Vector3(RandomValue() * 6, 0, side * 8), bounce = .35f, halfHeight = .033f,
            };
            void MoveHit()
            {
                var p = item.transform.position;
                hit.transform.position = new Vector3(p.x, .15f, p.z);
            }
            physics.Landed += impulse => { PlayAt("thud", physics.Body.position); MoveHit(); };
            physics.Slept += MoveHit;
            Say("(Cộp! Một món hàng rơi khỏi kệ giữa, lăn ra lối đi.)", 3);
            AddTask("fallen", "Nhặt món hàng rơi, đặt lại lên kệ (khách có thể vấp)");
        }

        void PickFallen(StoreMarker hit)
        {
            if (!hit || !looseStock.TryGetValue(hit, out var item) || carry.Kind != null) return;
            looseStock.Remove(hit);
            RemoveProp(item);
            RemoveProp(hit.gameObject);
            PlayClip("shelf");
            Say("Đã đặt món hàng lại lên kệ.", 2);
            if (looseStock.Count == 0) CompleteTask("fallen");
        }

        void BeginDuplicate(float seconds)
        {
            duplicateTime = seconds + 5 * Mathf.Min(3, Run.Progress.Mercy.GetValueOrDefault("r7"));
            var views = new[] { new Vector3(2, .6f, -2.2f), new Vector3(3.4f, -2.2f, -1), new Vector3(1.2f, .9f, -3.4f) };
            var p = views[RandomIndex(views.Length)];
            if (!cctvDouble) cctvDouble = CameraActor(new ActorOptions { coat = "uniform", color = 0x00a98f, hair = 0x1a1410, face = "none" });
            cctvDouble.pos = new Vector3(p.x, 0, p.z);
            cctvDouble.yaw = p.y;
            Run.Progress.Flags["double"] = 1;
            PlayClip("beep");
            Say("(Màn hình camera nhấp nháy...)", 3);
        }

        Actor CameraActor(ActorOptions look)
        {
            var actor = Actor.Spawn(actorCatalog, look, Vector3.zero);
            actor.AutoTick = false;
            actor.gameObject.SetActive(false);
            return actor;
        }

        void RenderSecurityFeed(bool doubled, bool ghost)
        {
            if (!securityCamera)
            {
                securityCamera = new GameObject("HanaMart CCTV").AddComponent<Camera>();
                securityCamera.enabled = false;
                securityCamera.fieldOfView = 70;
                securityCamera.aspect = 1.6f;
                securityCamera.nearClipPlane = .1f;
                securityCamera.farClipPlane = 40;
                securityCamera.clearFlags = CameraClearFlags.SolidColor;
                securityCamera.backgroundColor = new Color32(5, 6, 10, 255);
                securityCamera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
                securityImage = new RenderTexture(256, 160, 16, RenderTextureFormat.ARGB32) { name = "HanaMart CCTV feed" };
                securityCamera.targetTexture = securityImage;
                monitor.GetPropertyBlock(tintBlock);
                tintBlock.SetTexture("_BaseMap", securityImage);
                tintBlock.SetTexture("_EmissionMap", securityImage);
                monitor.SetPropertyBlock(tintBlock);
            }
            bool storage = ++cameraTick / 22 % 2 == 1;
            Vector3 position = storage ? new Vector3(4.3f, 2.75f, -.3f) : new Vector3(11.6f, 2.75f, 7.6f);
            Vector3 focus = storage ? new Vector3(1.2f, .6f, -3.2f) : new Vector3(4, .8f, 2.5f);
            securityCamera.transform.SetPositionAndRotation(ThreeCoords.ToUnity(position), Quaternion.LookRotation(ThreeCoords.ToUnity(focus - position)));
            if (doubled && cctvDouble)
            {
                cctvDouble.gameObject.SetActive(true);
                cctvDouble.StepPose(0, 0);
            }
            if (ghost)
            {
                if (!cctvGhost) cctvGhost = CameraActor(new ActorOptions { color = 0x101114, mask = "white", face = "wide", pale = true, uncanny = 1 });
                cctvGhost.gameObject.SetActive(true);
                cctvGhost.pos = new Vector3(7.2f, 0, 1.3f);
                cctvGhost.yaw = Mathf.Atan2(position.x - 7.2f, position.z - 1.3f);
                cctvGhost.StepPose(.18f, 0);
            }
            monitor.enabled = false;
            try { securityCamera.Render(); }
            finally
            {
                monitor.enabled = true;
                if (cctvDouble) cctvDouble.gameObject.SetActive(false);
                if (cctvGhost) cctvGhost.gameObject.SetActive(false);
            }
        }

        void PlayAt(string id, Vector3 position)
        {
            var clip = audioBank ? audioBank.Find(id) : null;
            if (clip) AudioSource.PlayClipAtPoint(clip, ThreeCoords.ToUnity(position), .7f);
        }

        StoreMarker NewHit(string id, string text, Vector3 position, Vector3 size)
        {
            var trigger = PropGroup("H:" + id, position);
            trigger.layer = LayerMask.NameToLayer("Interact");
            var box = trigger.AddComponent<BoxCollider>();
            box.size = size;
            box.isTrigger = true;
            var marker = trigger.AddComponent<StoreMarker>();
            marker.key = "H:" + id;
            marker.kind = StoreMarker.Kind.Hit;
            marker.json = JsonUtility.ToJson(new HitLabel { id = id, label = text });
            return marker;
        }

        GameObject PropGroup(string name, Vector3 position)
        {
            var item = new GameObject(name);
            item.transform.position = ThreeCoords.ToUnity(position);
            dynamics.Add(item);
            return item;
        }

        void RemoveProp(GameObject item)
        {
            if (!item) return;
            dynamics.Remove(item);
            item.SetActive(false);
            Destroy(item);
        }

        void ChangeTint(Renderer renderer, int rgb)
        {
            renderer.GetPropertyBlock(tintBlock);
            tintBlock.SetColor("_BaseColor", Hex(rgb));
            renderer.SetPropertyBlock(tintBlock);
        }

        void Power(bool inside, bool outside)
        {
            insidePower = inside;
            outsidePower = outside;
            ApplyAmbient(inside ? litAmbient : .06f, inside ? litTint : Color.white);
            foreach (var light in lamps)
                if (light.Lamp) ApplyLamp(light, light.Off || !(light.Inside ? inside : outside) ? 0 : 1);
        }

        void TintStore(int rgb, float environment)
        {
            litTint = Hex(rgb);
            litAmbient = environment;
            foreach (var light in lamps)
            {
                if (!light.Lamp || !light.Inside) continue;
                light.Lamp.color = light.Color * litTint;
                ApplyLamp(light, light.Off || !insidePower ? 0 : 1);
            }
            ApplyAmbient(insidePower ? litAmbient : .04f, insidePower ? litTint : Color.white);
        }

        void ApplyAmbient(float level, Color tint)
        {
            ambientLevel = level;
            ambientTint = tint;
            if (hemisphere)
            {
                hemisphere.scale = level;
                hemisphere.tint = tint;
                hemisphere.Apply();
            }
            else RenderSettings.ambientIntensity = baseAmbient * level;
            RenderSettings.reflectionIntensity = baseReflection * level;
        }

        void ApplyLamp(ShopLight state, float multiplier)
        {
            if (state.Lamp) state.Lamp.intensity = state.Brightness * multiplier;
            if (state.Panels.Count == 0) return;
            tintBlock ??= new MaterialPropertyBlock();
            foreach (var panel in state.Panels)
            {
                if (!panel.renderer) continue;
                if (multiplier >= .999f && litTint == Color.white)
                {
                    panel.renderer.SetPropertyBlock(null);
                    continue;
                }
                tintBlock.Clear();
                if (multiplier <= .001f)
                {
                    tintBlock.SetColor("_BaseColor", new Color(.12f, .14f, .16f));
                    tintBlock.SetColor("_EmissionColor", Color.black);
                }
                else
                {
                    tintBlock.SetColor("_BaseColor", panel.baseColor * litTint);
                    tintBlock.SetColor("_EmissionColor", panel.emissionColor * litTint * multiplier);
                }
                panel.renderer.SetPropertyBlock(tintBlock);
            }
        }

        static Color Hex(int rgb) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);

        static Material Lit(int rgb, float smoothness)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = Hex(rgb) };
            material.SetFloat("_Smoothness", smoothness);
            return material;
        }

        static void MakeTransparent(Material material)
        {
            material.renderQueue = 3000;
            material.SetFloat("_Surface", 1);
            material.SetFloat("_SrcBlend", 5);
            material.SetFloat("_DstBlend", 10);
            material.SetFloat("_ZWrite", 0);
            material.SetFloat("_Cull", 0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
    }
}
