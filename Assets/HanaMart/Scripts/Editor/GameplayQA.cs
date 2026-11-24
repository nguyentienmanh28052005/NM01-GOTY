using System.IO;
using System.Linq;
using HanaMart.Player;
using HanaMart.Store;
using UnityEditor;
using UnityEngine;

namespace HanaMart.EditorTools
{
    /// Reproducible runtime inspection through MCP execute_menu_item, without desktop input.
    [InitializeOnLoad]
    public static class GameplayQA
    {
        static GameplayQA()
        {
            Application.logMessageReceived += (message, stack, type) =>
            {
                if (!Application.isPlaying || (type != LogType.Error && type != LogType.Exception && type != LogType.Assert && type != LogType.Warning)) return;
                Directory.CreateDirectory("Logs/HanaMart");
                File.AppendAllText("Logs/HanaMart/play-errors.log", $"{System.DateTime.UtcNow:O} [{type}] {message}\n{stack}\n");
            };
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingEditMode)
                { Directory.CreateDirectory("Logs/HanaMart"); File.WriteAllText("Logs/HanaMart/play-errors.log", ""); }
            };
        }
        [MenuItem("HanaMart/QA/Reload Scripts")]
        public static void ReloadScripts()
        {
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
            EditorUtility.RequestScriptReload();
        }
        [MenuItem("HanaMart/QA/Open Doors")]
        public static void OpenDoors()
        {
            if (!Application.isPlaying) return;
            Application.runInBackground = true;
            var doors = Object.FindFirstObjectByType<StoreDoors>();
            doors.Front.motor.target = doors.Back.motor.target = doors.Freezer.motor.target = 1;
            foreach (var leaf in doors.Fridges.Concat(doors.Microwaves)) leaf.motor.target = 1;
        }
        [MenuItem("HanaMart/QA/Face Rules")]
        public static void FaceRules()
        {
            if (!Application.isPlaying) return;
            Application.runInBackground = true;
            var player = Camera.main.GetComponent<PlayerController>();
            var rules = Object.FindObjectsByType<StoreMarker>(FindObjectsSortMode.None).First(m => m.key == "H:rules");
            var target = ThreeCoords.ToUnity(rules.transform.position);
            player.Motor.pos = new Vector2(target.x + .8f, target.z);
            player.Motor.SetYawFacing(-1, 0);
            player.Motor.pitch = Mathf.Atan2(target.y - 1.6f, .8f);
            player.Motor.Update(0, default, player.settings, player.Area); player.ApplyPose();
        }

        [MenuItem("HanaMart/QA/Runtime Snapshot")]
        public static void Snapshot()
        {
            var cam = Camera.main;
            var p = cam.GetComponent<PlayerController>();
            var interact = cam.GetComponent<HanaMart.Interaction.Interactor>();
            Directory.CreateDirectory("Logs/HanaMart");
            File.WriteAllText("Logs/HanaMart/runtime.json", JsonUtility.ToJson(new SnapshotData
            {
                playing = Application.isPlaying, cameraPosition = cam.transform.position,
                cameraEuler = cam.transform.eulerAngles, collisionBoxes = p.collisionBoxes.Count,
                inputEnabled = p.Input && p.Input.enabled, cursorLocked = Cursor.lockState == CursorLockMode.Locked,
                interaction = interact ? interact.CurrentId : null, label = interact ? interact.Label : null,
                hitsActive = Object.FindObjectsByType<StoreMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None).Count(m => m.kind == StoreMarker.Kind.Hit && m.gameObject.activeInHierarchy),
                rayTarget = interact && interact.Trace(cam.ViewportPointToRay(new Vector3(.5f,.5f,0))) ? interact.Trace(cam.ViewportPointToRay(new Vector3(.5f,.5f,0))).key : null
            }, true));
        }
        [System.Serializable] class SnapshotData
        {
            public bool playing, inputEnabled, cursorLocked;
            public Vector3 cameraPosition, cameraEuler;
            public int collisionBoxes;
            public int hitsActive;
            public string interaction, label;
            public string rayTarget;
        }
    }
}
