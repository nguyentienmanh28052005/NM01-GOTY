using System;
using System.Collections;
using System.IO;
using HanaMart.Actors;
using HanaMart.Player;
using HanaMart.Store;
using UnityEditor;
using UnityEngine;

namespace HanaMart.EditorTools
{
    public static class ActorRouteQA
    {
        [MenuItem("HanaMart/QA/Run Actor Route")]
        public static void Run()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Actor route QA runs in Play mode.");
            Application.runInBackground = true;
            var camera = Camera.main;
            camera.GetComponent<PlayerController>().StartCoroutine(CheckRoute(camera));
        }

        static IEnumerator CheckRoute(Camera camera)
        {
            var player = camera.GetComponent<PlayerController>();
            var doors = UnityEngine.Object.FindFirstObjectByType<StoreDoors>();
            var catalog = AssetDatabase.LoadAssetAtPath<ActorCatalog>(ActorSetup.CatalogPath);
            var end = new Vector2(2.15f, -7);
            var actor = Actor.Spawn(catalog, new ActorOptions { coat = "raincoat", color = 0xf2c200, hood = true },
                new Vector3(3.1f, 0, 11.6f));
            player.GameplayEnabled = false;
            player.Motor.frozen = true;
            player.Motor.vel = Vector2.zero;
            doors.Front.motor.target = doors.Back.motor.target = 1;
            if (!actor.TryMoveTo(player.Navigation, end)) throw new InvalidOperationException("No street-to-alley route.");

            var views = new[]
            {
                (z: 8.3f, viewer: new Vector2(3.15f, 10.45f), name: "entrance"),
                (z: 4.4f, viewer: new Vector2(.85f, 4.4f), name: "counter"),
                (z: -2.35f, viewer: new Vector2(1.75f, -.65f), name: "storage"),
                (z: -6.85f, viewer: new Vector2(3.8f, -6.3f), name: "alley"),
            };
            Directory.CreateDirectory("docs/visual/unity");
            Directory.CreateDirectory("Logs/HanaMart");
            File.Delete("Logs/HanaMart/actor-route.json");
            var report = new RouteReport();
            var last = new Vector2(actor.pos.x, actor.pos.z);
            double start = Time.realtimeSinceStartupAsDouble;
            try
            {
                for (int frame = 0; frame < 60000 && Time.realtimeSinceStartupAsDouble - start < 60; ++frame)
                {
                    yield return null;
                    var next = new Vector2(actor.pos.x, actor.pos.z);
                    if (!player.Navigation.LineFree(last, next))
                        throw new InvalidOperationException($"Actor crossed a blocked cell at {next:F4}.");
                    report.frames++;
                    report.distance += Vector2.Distance(last, next);
                    last = next;
                    if (report.photos < views.Length && actor.pos.z <= views[report.photos].z)
                    {
                        var view = views[report.photos++];
                        FaceActor(player, view.viewer, actor.pos);
                        CharacterPreview.CaptureFrame(camera, $"docs/visual/unity/actor-route-{view.name}.png");
                    }
                }
                report.seconds = (float)(Time.realtimeSinceStartupAsDouble - start);
                report.end = last;
                report.passed = report.seconds >= 60 && report.photos == views.Length &&
                    Vector2.Distance(last, end) < .01f && actor.Motion.blend < .001f;
                File.WriteAllText("Logs/HanaMart/actor-route.json", JsonUtility.ToJson(report, true));
                if (!report.passed) throw new InvalidOperationException("Actor route QA did not reach its endpoint and rest.");
            }
            finally { actor.Remove(); }
        }

        static void FaceActor(PlayerController player, Vector2 camera, Vector3 actor)
        {
            var delta = new Vector2(actor.x, actor.z) - camera;
            player.Motor.pos = camera;
            player.Motor.eye = 1.62f;
            player.Motor.SetYawFacing(delta.x, delta.y);
            player.Motor.pitch = Mathf.Atan2(.98f - player.Motor.eye, delta.magnitude);
            player.Motor.Update(0, default, player.settings, player.Area);
            player.ApplyPose();
        }

        [Serializable]
        sealed class RouteReport
        {
            public bool passed;
            public int frames, photos;
            public float seconds, distance;
            public Vector2 end;
        }
    }
}
