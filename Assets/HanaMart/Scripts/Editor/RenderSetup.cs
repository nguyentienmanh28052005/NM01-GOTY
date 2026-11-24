using System.Linq;
using HanaMart.Render;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HanaMart.EditorTools
{
    /// Gắn hậu kỳ giống bản Three.js: Bloom (UnrealBloom 0.3/0.5/1.05), tắt tone mapping URP, AgX + Film qua Full Screen Pass.
    public static class RenderSetup
    {
        public const string MaterialPath = "Assets/HanaMart/Art/Materials/AgxFilm.mat";
        public const string ProfilePath = "Assets/HanaMart/Settings/HanaMartVolume.asset";
        const string RendererPath = "Assets/Settings/PC_Renderer.asset";

        [MenuItem("HanaMart/Setup Rendering")]
        public static void Run()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (mat == null) { mat = new Material(Shader.Find("HanaMart/AgxFilm")); AssetDatabase.CreateAsset(mat, MaterialPath); }

            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            var feat = data.rendererFeatures.OfType<FullScreenPassRendererFeature>().FirstOrDefault(f => f.name == "AgxFilm");
            if (feat == null)
            {
                feat = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>(); feat.name = "AgxFilm";
                AssetDatabase.AddObjectToAsset(feat, data);
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feat, out _, out long localId);
                var so = new SerializedObject(data);
                var feats = so.FindProperty("m_RendererFeatures"); var map = so.FindProperty("m_RendererFeatureMap");
                feats.arraySize++; feats.GetArrayElementAtIndex(feats.arraySize - 1).objectReferenceValue = feat;
                map.arraySize++; map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
                so.ApplyModifiedProperties();
            }
            feat.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.AfterRenderingPostProcessing;
            feat.fetchColorBuffer = true;
            feat.requirements = ScriptableRenderPassInput.Color;
            feat.passMaterial = mat; feat.passIndex = 0;
            feat.SetActive(true);
            EditorUtility.SetDirty(feat); EditorUtility.SetDirty(data);

            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null) { profile = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile, ProfilePath); }
            var tm = Get<Tonemapping>(profile); tm.mode.Override(TonemappingMode.None);
            var bloom = Get<Bloom>(profile); bloom.intensity.Override(0.3f); bloom.scatter.Override(0.5f); bloom.threshold.Override(1.05f);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
        }

        static T Get<T>(VolumeProfile p) where T : VolumeComponent
        {
            if (!p.TryGet<T>(out var c)) { c = p.Add<T>(true); AssetDatabase.AddObjectToAsset(c, p); }
            c.active = true; return c;
        }

        /// Gắn profile vào Global Volume và FilmSettings vào camera của scene đang mở.
        public static void ApplyToScene(UnityEngine.SceneManagement.Scene scene)
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var v in root.GetComponentsInChildren<Volume>(true)) if (v.isGlobal) v.sharedProfile = profile;
                foreach (var cam in root.GetComponentsInChildren<Camera>(true))
                    if (cam.CompareTag("MainCamera") && !cam.GetComponent<FilmSettings>()) cam.gameObject.AddComponent<FilmSettings>();
            }
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
