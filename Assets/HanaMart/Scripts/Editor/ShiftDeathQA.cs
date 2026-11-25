using System;
using System.Collections;
using System.IO;
using System.Linq;
using HanaMart.Actors;
using HanaMart.Interaction;
using HanaMart.Player;
using HanaMart.Render;
using HanaMart.Shift;
using HanaMart.Store;
using UnityEditor;
using UnityEngine;

namespace HanaMart.EditorTools
{
    public static class ShiftDeathQA
    {
        [MenuItem("HanaMart/QA/Run Death Camera")]
        public static void Run()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Death camera QA runs in Play mode.");
            Application.runInBackground = true;
            var camera = Camera.main;
            camera.GetComponent<PlayerController>().StartCoroutine(Check(camera));
        }

        [MenuItem("HanaMart/QA/Capture Guest Death Cuts")]
        public static void GuestCuts()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Guest death QA runs in Play mode.");
            Application.runInBackground = true;
            var camera = Camera.main;
            camera.GetComponent<PlayerController>().StartCoroutine(CheckGuestCuts(camera));
        }

        [MenuItem("HanaMart/QA/Capture Object Death Cuts")]
        public static void ObjectCuts()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Object death QA runs in Play mode.");
            Application.runInBackground = true;
            var camera = Camera.main;
            camera.GetComponent<PlayerController>().StartCoroutine(CheckObjectCuts(camera));
        }

        [MenuItem("HanaMart/QA/Run Shift Cinematics")]
        public static void ShiftCinematicsQA()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Shift cinematics QA runs in Play mode.");
            Application.runInBackground = true;
            var camera = Camera.main;
            camera.GetComponent<PlayerController>().StartCoroutine(CheckShiftCinematics(camera));
        }

        static IEnumerator CheckShiftCinematics(Camera view)
        {
            const string output = "Logs/HanaMart/shift-cinematics.json";
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.Delete(output);
            var game = view.GetComponent<ShiftGame>();
            var player = view.GetComponent<PlayerController>();

            game.NewShift();
            yield return null;
            yield return null;

            game.TriggerCinematic(1.2f);
            yield return null;
            if (!game.Cinematics.Active || game.Cinematics.Slowmo >= 1f || game.Cinematics.Dolly >= 0f || !game.Letterbox)
                throw new InvalidOperationException("Mid-shift cinematic did not activate slowmo, dolly, and letterbox.");

            game.ShowPaper("KIỂM TRA", "Giấy thử nghiệm giữa đoạn điện ảnh.");
            yield return null;
            game.ClosePaper();
            game.ShowDialog("KIỂM TRA", "Hộp thoại giữa đoạn điện ảnh.", new ShiftGame.Choice("Tiếp tục", null));
            yield return null;
            game.SelectOption(0);

            double started = Time.realtimeSinceStartupAsDouble;
            double deadline = started + 2.0;
            for (int frame = 0; frame < 1000 && game.Cinematics.Active && Time.realtimeSinceStartupAsDouble < deadline; ++frame)
                yield return null;
            if (game.Cinematics.Active || Mathf.Abs(game.Cinematics.Slowmo - 1f) > .001f || Mathf.Abs(game.Cinematics.Dolly) > .001f ||
                game.Letterbox || !player.GameplayEnabled)
                throw new InvalidOperationException("Mid-shift cinematic did not finish cleanly or stalled player input.");

            if (!game.SpawnApparition() || game.ActiveApparitions != 1)
                throw new InvalidOperationException("Apparition did not spawn in peripheral view.");
            var ghost = GameObject.Find("Bóng rìa mắt")?.GetComponent<Actor>();
            if (!ghost) throw new InvalidOperationException("Apparition actor missing.");
            player.Motor.SetYawFacing(ghost.pos.x - player.Motor.pos.x, ghost.pos.z - player.Motor.pos.y);
            player.Motor.Update(0, default, player.settings, player.Area);
            player.ApplyPose();
            yield return null;
            yield return null;
            if (game.ActiveApparitions != 0 || game.Cinematics.Pulse <= .2f || game.Cinematics.Shake <= .05f)
                throw new InvalidOperationException("Looking at apparition did not dismiss it with pulse/shake spike.");

            game.NewShift();
            yield return null;
            yield return null;
            if (Mathf.Abs(game.Cinematics.Slowmo - 1f) > .001f || game.Letterbox || game.Cinematics.Pulse != 0f || !player.GameplayEnabled)
                throw new InvalidOperationException("NewShift did not reset cinematics state.");

            File.WriteAllText(output, "{\"passed\":true}");
        }

        static IEnumerator CheckObjectCuts(Camera view)
        {
            var game = view.GetComponent<ShiftGame>();
            var player = view.GetComponent<PlayerController>();
            var doors = UnityEngine.Object.FindFirstObjectByType<StoreDoors>();
            var allMarkers = UnityEngine.Object.FindObjectsByType<StoreMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var lightMarkers = allMarkers.Where(marker => marker.kind == StoreMarker.Kind.Light)
                .Select(marker => (marker, lamp: marker.GetComponentInChildren<Light>(true)))
                .Where(entry => entry.lamp != null).ToArray();
            var insideCeilings = lightMarkers.Where(entry =>
                !entry.marker.key.Contains("alley", StringComparison.OrdinalIgnoreCase) &&
                entry.lamp.transform.position.y > 2.5f &&
                ThreeCoords.ToUnity(entry.lamp.transform.position) is Vector3 pos &&
                pos.x >= 0 && pos.x <= 12 && pos.z < 8 && pos.z >= -4.6f).Select(entry => entry.lamp).ToArray();
            var storageLight = insideCeilings.First(lamp => lamp.transform.position.z < 0);
            var alleyLights = lightMarkers.Where(entry => entry.marker.key.Contains("alley", StringComparison.OrdinalIgnoreCase))
                .Select(entry => entry.lamp).ToArray();
            var initialColors = insideCeilings.Select(lamp => lamp.color).ToArray();
            var monitor = allMarkers.FirstOrDefault(marker => marker.key == "R:monScreen")?.GetComponent<Renderer>();
            var freezerLid = doors ? doors.Freezer : null;
            var hemisphere = UnityEngine.Object.FindFirstObjectByType<HemisphereAmbient>();

            var cuts = new[]
            {
                (rule: 12, at: 5.25f, pos: default(Vector2?), facing: default(Vector2?)),
                (rule: 16, at: 5.4f, pos: null, facing: null),
                (rule: 6, at: 2.30f, pos: new Vector2(3, -2.8f), facing: new Vector2(0, -1)),
                (rule: 7, at: 5.6f, pos: null, facing: null),
                (rule: 8, at: 2.6f, pos: new Vector2(2.8f, -6.25f), facing: new Vector2(0, 1)),
                (rule: 21, at: 2.6f, pos: null, facing: null),
                (rule: 5, at: 2.85f, pos: new Vector2(2.15f, -6.2f), facing: new Vector2(0, -1)),
                (rule: 10, at: 4.85f, pos: null, facing: null),
                (rule: 14, at: 2.55f, pos: null, facing: null),
                (rule: 17, at: 1.6f, pos: null, facing: null),
                (rule: 0, at: 1.55f, pos: null, facing: null),
            };

            var results = new CutResult[cuts.Length];
            Directory.CreateDirectory("docs/visual/unity");
            Directory.CreateDirectory("Logs/HanaMart");
            const string output = "Logs/HanaMart/death-objects.json";
            File.Delete(output);

            for (int index = 0; index < cuts.Length; ++index)
            {
                var (rule, at, pos, facing) = cuts[index];
                game.NewShift();
                if (pos.HasValue && facing.HasValue)
                {
                    player.Motor.pos = pos.Value;
                    player.Motor.SetYawFacing(facing.Value.x, facing.Value.y);
                    player.Motor.pitch = player.Motor.crouch = 0;
                    player.Motor.Update(0, default, player.settings, player.Area);
                    player.ApplyPose();
                }
                yield return null;
                yield return null;
                int productsBefore = allMarkers.Count(marker => marker.kind == StoreMarker.Kind.Product && marker.gameObject.activeInHierarchy);
                game.Die(rule, "Kiểm tra cảnh thua thiết bị và địa điểm.");
                var cut = game.ActiveDeath;
                double end = Time.realtimeSinceStartupAsDouble + 25;
                for (int frame = 0; frame < 5000 && cut.Time < at && Time.realtimeSinceStartupAsDouble < end; ++frame) yield return null;
                if (cut.Done || cut.Time < at) throw new InvalidOperationException($"Rule {rule} did not reach its capture cue.");

                var actors = UnityEngine.Object.FindObjectsByType<Actor>(FindObjectsSortMode.None)
                    .Where(actor => actor.name.StartsWith("Cảnh thua", StringComparison.Ordinal)).ToArray();
                bool staged = rule switch
                {
                    12 => game.PosDisplay == "04:44\n04:44\n04:44" && actors.Length == 1,
                    16 => game.PosDisplay == "\nSAU LƯNG\n" && actors.Length == 1,
                    6 => insideCeilings.Length > 0 && insideCeilings.All(lamp => lamp.color.b > lamp.color.r * 1.15f) &&
                         UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                             .Count(t => t.name == "Tay trong tủ đông") == 6 &&
                         freezerLid != null && Quaternion.Angle(freezerLid.closed, freezerLid.transform.localRotation) > 60f,
                    7 => actors.Length == 1,
                    8 => GameObject.Find("Thùng thứ 13") is GameObject alleyBox &&
                         Vector3.Distance(alleyBox.transform.position, ThreeCoords.ToUnity(new Vector3(3, 0, -5.55f))) < .05f &&
                         actors.Length == 1 && storageLight.intensity == 0,
                    21 => GameObject.Find("Thùng thứ 13") is GameObject storeBox &&
                          Vector3.Distance(storeBox.transform.position, ThreeCoords.ToUnity(new Vector3(3, 0, -5.55f))) > 1f &&
                          actors.Length == 1 && storageLight.intensity == 0,
                    5 => actors.Length == 1 && actors[0].pos.z > -9f && actors[0].pos.z < -4.7f &&
                         alleyLights.Length > 0 && alleyLights.All(lamp => lamp.intensity > 0),
                    10 => insideCeilings.Length > 0 && insideCeilings.All(lamp => lamp.intensity == 0) &&
                          (!hemisphere || hemisphere.scale < .1f) &&
                          actors.Length == 1 && GameObject.Find("Tia lửa cầu dao") != null,
                    14 => actors.Length == 7 && insideCeilings.Length > 0 && insideCeilings.All(lamp => lamp.color.r > lamp.color.g * 2f),
                    17 => actors.Length == 1 && Mathf.Abs(actors[0].roll - Mathf.PI) < .01f && GameObject.Find("Tia lửa đèn trần") != null,
                    0 => allMarkers.Count(marker => marker.kind == StoreMarker.Kind.Product && marker.gameObject.activeInHierarchy) < productsBefore &&
                         UnityEngine.Object.FindObjectsByType<ItemPhysicsBody>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length > 0,
                    _ => false,
                };
                if (!staged) throw new InvalidOperationException($"Rule {rule} did not satisfy its source staging assertions.");

                CharacterPreview.CaptureFrame(view, $"docs/visual/unity/death-{rule}.png");
                for (int frame = 0; frame < 6500 && !cut.Done && Time.realtimeSinceStartupAsDouble < end; ++frame) yield return null;
                if (!cut.Done || cut.Skipped || cut.Time >= 19.9f || game.Dialogue == null)
                    throw new InvalidOperationException($"Rule {rule} did not finish its full sequence cleanly.");
                results[index] = new CutResult { rule = rule, kind = cut.Kind, seconds = cut.Time };

                game.SelectOption(0);
                yield return null;
                yield return null;

                if (game.Dead || !player.GameplayEnabled || actors.Any(actor => actor))
                    throw new InvalidOperationException($"Rule {rule} left death state or actor after retry.");
                if (!insideCeilings.All(lamp => lamp.intensity > 0) || !alleyLights.All(lamp => lamp.intensity > 0))
                    throw new InvalidOperationException($"Rule {rule} did not restore store/alley lights after retry.");
                for (int l = 0; l < insideCeilings.Length; ++l)
                    if (Vector4.Distance(insideCeilings[l].color, initialColors[l]) > .01f)
                        throw new InvalidOperationException($"Rule {rule} did not restore lamp color after retry.");
                if (hemisphere && (Mathf.Abs(hemisphere.scale - 1) > .01f || Vector4.Distance(hemisphere.tint, Color.white) > .01f))
                    throw new InvalidOperationException($"Rule {rule} did not restore HemisphereAmbient after retry.");
                if ((rule == 12 || rule == 16) && !string.IsNullOrEmpty(game.PosDisplay))
                    throw new InvalidOperationException($"Rule {rule} did not restore POS display after retry.");
                if (rule == 6 && (GameObject.Find("Tay trong tủ đông") != null || GameObject.Find("Lòng tủ đông") != null ||
                    (freezerLid != null && Quaternion.Angle(freezerLid.closed, freezerLid.transform.localRotation) > 1f)))
                    throw new InvalidOperationException("Rule 6 did not restore freezer arms or lid after retry.");
                if (rule == 7 && monitor != null)
                {
                    var block = new MaterialPropertyBlock();
                    monitor.GetPropertyBlock(block);
                    if (block.GetColor("_BaseColor").r < .99f)
                        throw new InvalidOperationException("Rule 7 did not restore CCTV monitor color after retry.");
                }
                if ((rule == 8 || rule == 21) && GameObject.Find("Thùng thứ 13") != null)
                    throw new InvalidOperationException($"Rule {rule} did not remove box 13 after retry.");
                if (rule == 10 && GameObject.Find("Tia lửa cầu dao") != null)
                    throw new InvalidOperationException("Rule 10 did not remove spark light after retry.");
                if (rule == 17 && GameObject.Find("Tia lửa đèn trần") != null)
                    throw new InvalidOperationException("Rule 17 did not remove ceiling spark light after retry.");
                if (rule == 0)
                {
                    int productsAfter = allMarkers.Count(marker => marker.kind == StoreMarker.Kind.Product && marker.gameObject.activeInHierarchy);
                    int bodiesAfter = UnityEngine.Object.FindObjectsByType<ItemPhysicsBody>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
                    if (productsAfter != productsBefore || bodiesAfter != 0)
                        throw new InvalidOperationException($"Rule 0 did not restore shelf products ({productsAfter}/{productsBefore}) or clear physics clones ({bodiesAfter}).");
                }
            }
            File.WriteAllText(output, JsonUtility.ToJson(new CutResults { passed = true, scenes = results }, true));
        }

        static IEnumerator CheckGuestCuts(Camera view)
        {
            var game = view.GetComponent<ShiftGame>();
            var cuts = new[] { (rule: 3, at: 2.2f), (rule: 18, at: 2.6f), (rule: 9, at: 4.1f), (rule: 13, at: 3.7f) };
            var results = new CutResult[cuts.Length];
            Directory.CreateDirectory("docs/visual/unity");
            Directory.CreateDirectory("Logs/HanaMart");
            const string output = "Logs/HanaMart/death-guests.json";
            File.Delete(output);
            for (int index = 0; index < cuts.Length; ++index)
            {
                var (rule, at) = cuts[index];
                game.NewShift();
                yield return null;
                yield return null;
                game.Die(rule, "Kiểm tra cảnh thua theo nội quy.");
                var cut = game.ActiveDeath;
                double end = Time.realtimeSinceStartupAsDouble + 24;
                for (int frame = 0; frame < 4000 && cut.Time < at && Time.realtimeSinceStartupAsDouble < end; ++frame) yield return null;
                if (cut.Done || cut.Time < at) throw new InvalidOperationException($"Rule {rule} did not reach its capture cue.");
                var actors = UnityEngine.Object.FindObjectsByType<Actor>(FindObjectsSortMode.None)
                    .Where(actor => actor.name.StartsWith("Cảnh thua", StringComparison.Ordinal)).ToArray();
                bool staged = rule switch
                {
                    3 => actors.Length == 1 && Mathf.Abs(actors[0].pos.x - 2.45f) < .01f,
                    18 => GameObject.Find("Kính nứt"),
                    9 => actors.Length == 1 && actors[0].pos.z < 3.4f,
                    13 => actors.Length == 7 && !string.IsNullOrEmpty(game.ClockCaption),
                    _ => false,
                };
                if (!staged) throw new InvalidOperationException($"Rule {rule} did not create its source staging.");
                CharacterPreview.CaptureFrame(view, $"docs/visual/unity/death-{rule}.png");
                for (int frame = 0; frame < 6500 && !cut.Done && Time.realtimeSinceStartupAsDouble < end; ++frame) yield return null;
                if (!cut.Done || cut.Skipped || cut.Time > 19.9f || game.Dialogue == null)
                    throw new InvalidOperationException($"Rule {rule} did not finish its full sequence.");
                results[index] = new CutResult { rule = rule, kind = cut.Kind, seconds = cut.Time };
                game.SelectOption(0);
                yield return null;
                yield return null;
                if (game.Dead || GameObject.Find("Kính nứt") || actors.Any(actor => actor))
                    throw new InvalidOperationException($"Rule {rule} left a scene actor or prop after retry.");
            }
            File.WriteAllText(output, JsonUtility.ToJson(new CutResults { passed = true, scenes = results }, true));
        }

        static IEnumerator Check(Camera view)
        {
            const string output = "Logs/HanaMart/death-camera.json";
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.Delete(output);
            var player = view.GetComponent<PlayerController>();
            var game = view.GetComponent<ShiftGame>();
            var film = view.GetComponent<FilmSettings>();
            game.NewShift();
            yield return null;
            Quaternion originalRotation = view.transform.rotation;
            game.Die(99, "Kiểm camera và phục hồi cảnh.");
            if (game.ActiveDeath == null || game.SkipDeath() || player.GameplayEnabled) throw new InvalidOperationException("The cut did not lock control for its opening.");
            double deadline = Time.realtimeSinceStartupAsDouble + 8;
            float rotation = 0;
            for (int frame = 0; frame < 4000 && !game.ActiveDeath.CanSkip && Time.realtimeSinceStartupAsDouble < deadline; ++frame)
            {
                rotation = Mathf.Max(rotation, Quaternion.Angle(originalRotation, view.transform.rotation));
                yield return null;
            }
            bool skipped = game.SkipDeath();
            if (film.cine < .99f || rotation < .8f || !skipped)
                throw new InvalidOperationException($"Death camera: cine={film.cine:F3}, rotation={rotation:F3}, elapsed={game.ActiveDeath.Time:F3}, skipped={skipped}.");
            yield return null;
            yield return null;
            if (film.fade < .99f || game.Options.Length != 1) throw new InvalidOperationException("Skipping the cut did not open the recap.");
            game.SelectOption(0);
            yield return null;
            yield return null;
            bool actorsGone = !UnityEngine.Object.FindObjectsByType<Actor>(FindObjectsSortMode.None).Any(actor => actor.name.StartsWith("Cảnh thua", StringComparison.Ordinal));
            if (!actorsGone || game.ActiveDeath != null || game.Dead || !player.GameplayEnabled || film.cine != 0 || film.fade != 0 || film.red != 0)
                throw new InvalidOperationException("Retry did not restore actor/film/input state.");
            File.WriteAllText(output, JsonUtility.ToJson(new Result { passed = true, cameraDegrees = rotation, restored = actorsGone }, true));
        }

        [Serializable]
        sealed class Result { public bool passed, restored; public float cameraDegrees; }

        [Serializable]
        sealed class CutResult { public int rule; public string kind; public float seconds; }
        [Serializable]
        sealed class CutResults { public bool passed; public CutResult[] scenes; }
    }
}
