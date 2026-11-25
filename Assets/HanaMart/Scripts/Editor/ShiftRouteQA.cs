using System;
using System.IO;
using HanaMart.Interaction;
using HanaMart.Player;
using HanaMart.Shift;
using HanaMart.Store;
using UnityEditor;
using UnityEngine;

namespace HanaMart.EditorTools
{
    public static class ShiftRouteQA
    {
        [MenuItem("HanaMart/QA/Run Player Store Route")]
        public static void Run()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Player route QA runs in Play mode.");
            Application.runInBackground = true;
            var camera = Camera.main;
            var player = camera.GetComponent<PlayerController>();
            var game = camera.GetComponent<ShiftGame>();
            var input = camera.GetComponent<Interactor>();
            var markers = UnityEngine.Object.FindObjectsByType<StoreMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var doors = UnityEngine.Object.FindFirstObjectByType<StoreDoors>();
            game.NewShift();
            game.Run.Clock.Running = false;
            player.GameplayEnabled = false;
            try
            {
                Move(player, new Vector2(.85f, 1.7f), new Vector2(3.1f, 1.7f), new Vector2(3.1f, 7.6f));
                doors.Front.motor.target = 1;
                doors.Tick(.2f);
                for (int frame = 0; frame < 60; ++frame) Steer(player, new Vector2(3.1f, 9.2f));
                float frontLimit = player.Motor.pos.y;
                if (frontLimit > 7.68f) throw new InvalidOperationException("Rule 1 did not keep the employee inside the front door.");

                Move(player, new Vector2(3.1f, 1.7f), new Vector2(.85f, 1.7f), new Vector2(.85f, -.7f),
                    new Vector2(1, -1), new Vector2(1, -1.64f), new Vector2(1.91f, -2.1f), new Vector2(1.92f, -4.15f));
                for (int frame = 0; frame < 60; ++frame) Steer(player, new Vector2(1.92f, -6));
                float backLimit = player.Motor.pos.y;
                if (backLimit < -4.3f) throw new InvalidOperationException("Employee crossed the closed back door.");
                ShiftTaskQA.Use(input, markers, "backDoor");
                doors.Tick(.3f);
                if (doors.Back.motor.target <= .5f) throw new InvalidOperationException("Rear hinge did not respond to the interaction.");
                Move(player, new Vector2(1.92f, -5.1f), new Vector2(2.15f, -7));

                Directory.CreateDirectory("Logs/HanaMart");
                File.WriteAllText("Logs/HanaMart/player-store-route.json", JsonUtility.ToJson(new Result
                    { passed = true, frontLimit = frontLimit, closedBackLimit = backLimit, alley = player.Motor.pos }, true));
            }
            finally { player.GameplayEnabled = true; }
        }

        static void Move(PlayerController player, params Vector2[] route)
        {
            foreach (var target in route)
            {
                for (int frame = 0; frame < 580 && Vector2.Distance(player.Motor.pos, target) >= .022f; ++frame) Steer(player, target);
                if (Vector2.Distance(player.Motor.pos, target) >= .03f)
                    throw new InvalidOperationException($"Employee did not reach {target:F3} from {player.Motor.pos:F3}.");
                player.Motor.vel = Vector2.zero;
            }
            player.ApplyPose();
        }

        static void Steer(PlayerController player, Vector2 target)
        {
            var motor = player.Motor;
            var delta = target - motor.pos;
            motor.SetYawFacing(delta.x, delta.y);
            float throttle = Mathf.Clamp(delta.magnitude * 12, .15f, 1);
            motor.Update(1f / 60, new PlayerInputState { move = new Vector2(0, -throttle) }, player.settings, player.Area);
        }

        [Serializable]
        sealed class Result { public bool passed; public float frontLimit, closedBackLimit; public Vector2 alley; }
    }
}
