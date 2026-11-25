using System.Collections;
using System.Linq;
using HanaMart.Actors;
using HanaMart.Store;
using UnityEngine;

namespace HanaMart.Shift
{
    public sealed partial class ShiftGame
    {
        Material spillMaterial, wetMaterial, printMaterial, steamMaterial;
        Texture2D printTexture, spillTexture, steamTexture, wetTexture;

        void ReleaseEffects()
        {
            foreach (var material in new[] { spillMaterial, wetMaterial, printMaterial, steamMaterial })
                if (material) Destroy(material);
            foreach (var texture in new[] { printTexture, spillTexture, steamTexture, wetTexture })
                if (texture) Destroy(texture);
        }

        void MakeSpill()
        {
            if (spill) return;
            if (!spillMaterial)
            {
                spillMaterial = Lit(0xffffff, .95f);
                MakeTransparent(spillMaterial);
                var drops = new Ellipse[9];
                for (int i = 0; i < drops.Length; ++i)
                    drops[i] = new Ellipse(128 + (RandomValue() - .5f) * 90, 128 + (RandomValue() - .5f) * 90,
                        30 + RandomValue() * 50, 20 + RandomValue() * 35, RandomValue() * 3);
                spillTexture = Stamp(256, drops, new Color32(120, 60, 20, 192));
                spillMaterial.mainTexture = spillTexture;
            }
            spill = Decal("Vết nước đổ", new Vector3(6, .006f, 4.6f), Quaternion.Euler(90, 0, 0), new Vector2(1.1f, 1.1f), spillMaterial);
            spillHit = NewHit("spill", "Vết nước đổ", new Vector3(6, .15f, 4.6f), new Vector3(1, .3f, 1));
            PlayClip("thud");
            AddTask("mop", "Lau vết nước đổ ở lối giữa (cây lau trong kho)");
        }

        void CleanSpill()
        {
            if (!spill) return;
            var origin = ThreeCoords.ToUnity(spill.transform.position);
            RemoveProp(spill);
            RemoveProp(spillHit.gameObject);
            spill = null;
            spillHit = null;
            if (!wetMaterial)
            {
                wetMaterial = Lit(0x1c2630, .96f);
                wetMaterial.SetFloat("_Metallic", .3f);
                MakeTransparent(wetMaterial);
                wetTexture = Stamp(128, new[] { new Ellipse(64, 64, 63.5f, 63.5f) }, Color.white);
                wetMaterial.mainTexture = wetTexture;
            }
            var patch = Decal("Sàn còn ướt", new Vector3(origin.x, .004f, origin.z), Quaternion.Euler(90, 0, 0), new Vector2(1.95f, 1.5f), wetMaterial);
            StartCoroutine(Dry(patch));
            CompleteTask("mop");
            PlayClip("mop");
            Say("Sàn còn ướt — sẽ khô dần.", 2);
        }

        IEnumerator Dry(GameObject patch)
        {
            var renderer = patch.GetComponent<Renderer>();
            var opacity = new MaterialPropertyBlock();
            const float dryTime = 40;
            for (float time = 0; time < dryTime; time += Time.deltaTime)
            {
                var color = Hex(0x1c2630);
                color.a = .32f * (1 - time / dryTime);
                opacity.SetColor("_BaseColor", color);
                renderer.SetPropertyBlock(opacity);
                yield return null;
            }
            RemoveProp(patch);
        }

        void Handprints()
        {
            if (printHit) return;
            if (!printMaterial)
            {
                var fingers = new Ellipse[6];
                fingers[0] = new Ellipse(64, 84, 26, 30);
                for (int i = 0; i < 5; ++i)
                {
                    float angle = -2.4f + i * .42f + (i == 0 ? -.35f : 0);
                    float x = 64 + Mathf.Cos(angle) * 30 + 16 * Mathf.Cos(angle);
                    float y = 84 + Mathf.Sin(angle) * 34 + 16 * Mathf.Sin(angle);
                    fingers[i + 1] = new Ellipse(x, y, 7, i == 0 ? 14 : 20, angle + Mathf.PI / 2);
                }
                printTexture = Stamp(128, fingers, new Color32(220, 235, 255, 112));
                printMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                printMaterial.mainTexture = printTexture;
                MakeTransparent(printMaterial);
            }
            int count = 6 + RandomIndex(5), generation = ++printGeneration;
            float x0 = 4.2f + RandomValue() * 5;
            for (int i = 0; i < count; ++i)
            {
                int index = i;
                Later(i * .38f, () =>
                {
                    if (Dead || generation != printGeneration) return;
                    var position = new Vector3(x0 + index * .28f + RandomValue() * .1f, .9f + RandomValue() * 1.3f, 8.035f);
                    var rotation = Quaternion.Euler(0, 180, -(RandomValue() - .5f) * .6f * Mathf.Rad2Deg);
                    prints.Add(Decal("Dấu tay ướt", position, rotation, new Vector2(.22f, .22f), printMaterial));
                    if (index % 3 == 0) PlayAt("tap", position);
                });
            }
            printHit = NewHit("prints", "Dấu tay", new Vector3(x0 + count * .14f, 1.6f, 7.85f), new Vector3(count * .3f + .4f, 1.6f, .3f));
            Say("(Lạch cạch trên mặt kính... dấu tay ướt in từ BÊN NGOÀI vào.)", 3);
            AddTask("prints", "Lau dấu tay trên cửa kính (cần cây lau)");
        }

        void CleanPrints()
        {
            if (!printHit) return;
            ++printGeneration;
            foreach (var print in prints) RemoveProp(print);
            prints.Clear();
            RemoveProp(printHit.gameObject);
            printHit = null;
            CompleteTask("prints");
            Say("Lau xong... nhưng dấu tay ở phía NGOÀI kính, sao lau từ trong lại sạch được?");
        }

        GameObject Decal(string name, Vector3 position, Quaternion rotation, Vector2 scale, Material material)
        {
            var decal = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var collider = decal.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            decal.name = name;
            decal.transform.SetPositionAndRotation(ThreeCoords.ToUnity(position), rotation);
            decal.transform.localScale = new Vector3(scale.x, scale.y, 1);
            decal.GetComponent<Renderer>().sharedMaterial = material;
            dynamics.Add(decal);
            return decal;
        }

        void SteamAt(Vector3 position, float seconds, float rate)
        {
            if (!steamMaterial)
            {
                steamTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                var pixels = new Color32[32 * 32];
                for (int y = 0; y < 32; ++y)
                    for (int x = 0; x < 32; ++x)
                    {
                        float alpha = Mathf.Pow(Mathf.Clamp01(1 - new Vector2(x - 15.5f, y - 15.5f).magnitude / 15.5f), 2);
                        pixels[y * 32 + x] = new Color32(235, 240, 248, (byte)(alpha * 210));
                    }
                steamTexture.SetPixels32(pixels);
                steamTexture.Apply();
                steamMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                steamMaterial.mainTexture = steamTexture;
                MakeTransparent(steamMaterial);
            }
            var root = PropGroup("Hơi nước", position);
            var particles = root.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.duration = Mathf.Max(.05f, seconds);
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.8f, 1.35f);
            main.startSize = new ParticleSystem.MinMaxCurve(.035f, .13f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(.12f, .38f);
            main.gravityModifier = -.02f;
            var emission = particles.emission;
            emission.rateOverTime = rate;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.radius = .07f;
            shape.angle = 15;
            shape.rotation = new Vector3(-90, 0, 0);
            root.GetComponent<ParticleSystemRenderer>().sharedMaterial = steamMaterial;
            particles.Play();
            Later(seconds + 1.4f, () => RemoveProp(root));
        }

        void SpawnStill()
        {
            var spots = new[] { new Vector2(5.9f, 1.2f), new Vector2(8.5f, 1.2f), new Vector2(11, 1.4f),
                new Vector2(5.9f, 6.2f), new Vector2(3.3f, .9f), new Vector2(8.5f, 6.2f) };
            var distant = spots.Where(spot => Vector2.Distance(spot, player.Motor.pos) > 4.5f).ToArray();
            if (distant.Length == 0) return;
            var location = distant[RandomIndex(distant.Length)];
            var profile = new Profile("Bóng người đứng im", new ActorOptions
                { coat = RandomValue() < .5f ? "padding" : "suit", color = 0x15161a, mask = "white", face = "wide", pale = true }, -1, null, null);
            var guest = SpawnGuest("still", profile, new Vector3(location.x, 0, location.y), false);
            StartCoroutine(WatchStill(guest));
        }

        IEnumerator WatchStill(Guest guest)
        {
            float seen = 0;
            for (float life = 30; life > 0 && guest.Actor.alive; life -= Time.deltaTime)
            {
                guest.Actor.lookTarget = ThreeCoords.ToUnity(transform.position);
                var to = guest.Actor.transform.position + 1.5f * Vector3.up - transform.position;
                bool inView = Vector3.Dot(transform.forward, to.normalized) > .8f;
                if (inView) seen += Time.deltaTime;
                if (to.magnitude < 3.2f || (seen > 1.2f && !inView)) break;
                yield return null;
            }
            RemoveGuest(guest);
            if (seen > .3f) PlayAt("whisper", ThreeCoords.ToUnity(transform.position - transform.forward));
        }

        readonly struct Ellipse
        {
            readonly float x, y, width, height, sin, cos;
            public Ellipse(float x, float y, float width, float height, float rotation = 0)
            {
                this.x = x; this.y = y; this.width = width; this.height = height;
                sin = Mathf.Sin(rotation); cos = Mathf.Cos(rotation);
            }
            public bool Contains(float u, float v)
            {
                float dx = u - x, dy = v - y;
                float nx = (dx * cos + dy * sin) / width, ny = (dy * cos - dx * sin) / height;
                return nx * nx + ny * ny <= 1;
            }
        }

        static Texture2D Stamp(int size, Ellipse[] forms, Color32 ink)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; ++y)
                for (int x = 0; x < size; ++x)
                    foreach (var form in forms)
                        if (form.Contains(x, size - 1 - y)) { pixels[y * size + x] = ink; break; }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }
    }
}
