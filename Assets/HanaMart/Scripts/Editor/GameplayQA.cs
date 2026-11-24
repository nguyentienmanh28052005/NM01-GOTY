using System.IO;
using System.Linq;
using System.Collections;
using HanaMart.Interaction;
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
        [MenuItem("HanaMart/QA/Carry Smoke")]
        public static void CarrySmoke()
        {
            if(!Application.isPlaying)return;
            Application.runInBackground=true;
            var carry=Camera.main.GetComponent<CarryController>();
            carry.StartCoroutine(CarrySmokeRun(carry,.65f,"carry-smoke.json"));
        }
        [MenuItem("HanaMart/QA/Carry Rapid Smoke")]
        public static void CarryRapidSmoke()
        {
            if(!Application.isPlaying)return;
            Application.runInBackground=true;
            var carry=Camera.main.GetComponent<CarryController>();
            carry.StartCoroutine(CarrySmokeRun(carry,.31f,"carry-rapid-smoke.json"));
        }
        static IEnumerator CarrySmokeRun(CarryController carry,float settle,string file)
        {
            var kinds=new[]{"boxRamen","boxMilk","mop","trash","heat","hot"};
            for(int index=0;index<kinds.Length;index++)
            {
                string kind=kinds[index];carry.SetCarry(kind);carry.BeginPlacing();
                if(!carry.Placing)throw new System.Exception("Preview missing: "+kind);
                carry.Drop(new Vector3(1.6f,1.02f,4));yield return new WaitForSeconds(settle);
                if(carry.DroppedItems.Count!=1)throw new System.Exception("Drop missing: "+kind);
                var item=carry.DroppedItems[0];
                if(Mathf.Abs(item.mesh.transform.position.y-1.02f-CarryController.HalfHeight(kind))>.001f)throw new System.Exception("Drop height: "+kind);
                carry.Use("dropped",item.hit.GetComponent<StoreMarker>());yield return new WaitForSeconds(.3f);
                if(carry.Kind!=kind || carry.DroppedItems.Count!=0)throw new System.Exception("Pickup failed: "+kind);
            }
            carry.SetCarry(null);
            Directory.CreateDirectory("Logs/HanaMart");File.WriteAllText("Logs/HanaMart/"+file,"{\"passed\":true,\"kinds\":6}");
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
