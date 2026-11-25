using HanaMart.Actors;
using HanaMart.Audio;
using HanaMart.Shift;
using HanaMart.Store;
using HanaMart.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HanaMart.EditorTools
{
    public static class ShiftSetup
    {
        [MenuItem("HanaMart/Setup Shift")]
        public static void Run()
        {
            if (Application.isPlaying) throw new System.InvalidOperationException("Set up the shift in Edit mode.");
            var scene = EditorSceneManager.OpenScene(StoreImporter.ScenePath, OpenSceneMode.Single);
            var camera = Camera.main;
            var game = camera.GetComponent<ShiftGame>() ?? camera.gameObject.AddComponent<ShiftGame>();
            var settings = new SerializedObject(game);
            settings.FindProperty("actorCatalog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ActorCatalog>(ActorSetup.CatalogPath);
            settings.FindProperty("audioBank").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioBank>(AudioSetup.BankPath);
            settings.FindProperty("products").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ProductCatalog>(ProductExtractor.CatalogPath);
            settings.ApplyModifiedPropertiesWithoutUndo();
            if (!camera.GetComponent<ShiftHud>()) camera.gameObject.AddComponent<ShiftHud>();
            var ui = camera.GetComponent<ShiftInterface>() ?? camera.gameObject.AddComponent<ShiftInterface>();
            var uiSettings = new SerializedObject(ui);
            uiSettings.FindProperty("sourceFont").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Font>("Assets/HanaMart/Art/Fonts/NotoSans-Regular.ttf");
            uiSettings.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);
        }
    }
}
