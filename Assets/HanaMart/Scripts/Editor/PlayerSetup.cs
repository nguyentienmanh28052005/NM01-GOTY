using System.Collections.Generic;
using System.IO;
using HanaMart.Player;
using HanaMart.Interaction;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HanaMart.EditorTools
{
    public static class PlayerSetup
    {
        public const string ControlsPath = "Assets/HanaMart/Input/HanaMartControls.inputactions";
        [MenuItem("HanaMart/Setup Player")]
        public static void Run()
        {
            // Explicit destination: never save the user's scenes or the recovered scene.
            var scene = EditorSceneManager.OpenScene(StoreImporter.ScenePath, OpenSceneMode.Single);
            var camera = Camera.main;
            var player = camera.GetComponent<PlayerController>() ?? camera.gameObject.AddComponent<PlayerController>();
            player.controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath);
            if (!camera.GetComponent<Interactor>()) camera.gameObject.AddComponent<Interactor>();
            if (!camera.GetComponent<InteractionPrompt>()) camera.gameObject.AddComponent<InteractionPrompt>();
            camera.GetComponent<InteractionPrompt>().sourceFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/HanaMart/Art/Fonts/NotoSans-Regular.ttf");
            if (!camera.GetComponent<CarryController>()) camera.gameObject.AddComponent<CarryController>();
            camera.GetComponent<CarryController>().catalog = AssetDatabase.LoadAssetAtPath<HanaMart.Store.ProductCatalog>(ProductExtractor.CatalogPath);
            player.collisionBoxes = new List<Box2>();
            var meta = JObject.Parse(File.ReadAllText(StoreImporter.MetaPath));
            foreach (JArray c in meta["colliders"])
                player.collisionBoxes.Add(new Box2((float)c[0], (float)c[1], (float)c[2], (float)c[3]));
            camera.transform.SetPositionAndRotation(new Vector3(-.85f, 1.6f, 4.4f), HanaMart.Store.ThreeCoords.CameraRotation(0, 0, 0));
            camera.fieldOfView = 72; camera.nearClipPlane = .05f;
            EditorUtility.SetDirty(player);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"Player ready: {player.collisionBoxes.Count} Three.js collision boxes");
        }
    }
}
