using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HanaMart.EditorTools
{
    /// Chuẩn bị 4 nhân vật Mixamo: tách texture nhúng, tạo material URP Lit (Specular) theo mẫu Ch31, rig Humanoid.
    public static class CharacterSetup
    {
        public static readonly string[] ModelPaths =
        {
            "Assets/Art/Model/Charactor/Female Walk - Lat Mat.fbx", // Ch31 — material đã đúng, chỉ đổi rig
            "Assets/Art/Model/Charactor/character (4).fbx",        // Ch08
            "Assets/Art/Model/Charactor/character (5).fbx",        // Ch01
            "Assets/Art/Model/Charactor/character (6).fbx",        // Ch42
        };

        const string OutRoot = "Assets/HanaMart/Art/Characters/";
        const string BodyTemplate = "Assets/Art/Model/Charactor/Ch31_body.mat";
        const string HairTemplate = "Assets/Art/Model/Charactor/Ch31_hair.mat";

        [MenuItem("HanaMart/Setup Characters")]
        public static void Run()
        {
            foreach (var path in ModelPaths) SetupModel(path);
            AssetDatabase.SaveAssets();
        }

        static void SetupModel(string path)
        {
            var imp = (ModelImporter)AssetImporter.GetAtPath(path);
            if (!path.Contains("Female Walk"))
            {
                string ch = CharacterId(path);
                string dir = OutRoot + ch;
                Directory.CreateDirectory(dir);
                if (!File.Exists($"{dir}/{ch}_1001_Diffuse.png")) { imp.ExtractTextures(dir); AssetDatabase.Refresh(); }
                MarkNormalMaps(dir);
                foreach (var name in SourceMaterialNames(path))
                {
                    var mat = BuildMaterial(dir, ch, name);
                    imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), name), mat);
                }
            }
            imp.animationType = ModelImporterAnimationType.Human;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            imp.SaveAndReimport();
        }

        static string CharacterId(string path)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var r = go.GetComponentInChildren<Renderer>(true);
            return r.name.Substring(0, 4); // "Ch08_Body" -> "Ch08"
        }

        static IEnumerable<string> SourceMaterialNames(string path)
        {
            var names = new SortedSet<string>();
            foreach (var r in AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<Renderer>(true))
                foreach (var m in r.sharedMaterials) if (m) names.Add(m.name);
            return names;
        }

        static void MarkNormalMaps(string dir)
        {
            foreach (var f in Directory.GetFiles(dir, "*_Normal.png"))
            {
                var ti = (TextureImporter)AssetImporter.GetAtPath(f.Replace('\\', '/'));
                if (ti.textureType == TextureImporterType.NormalMap) continue;
                ti.textureType = TextureImporterType.NormalMap; ti.SaveAndReimport();
            }
        }

        /// Bộ texture cho slot: "xx_body" → 1001, "xx_body1" → 1002, "xx_hair" → bộ có số lớn nhất có Diffuse.
        public static int TextureSetFor(string materialName, IList<int> availableSets)
        {
            string n = materialName.ToLowerInvariant();
            if (n.Contains("hair")) return availableSets[availableSets.Count - 1];
            int i = n.LastIndexOf("body");
            string suffix = i >= 0 ? n.Substring(i + 4) : "";
            int k = int.TryParse(suffix, out var v) ? v : 0;
            return availableSets[Mathf.Min(k, availableSets.Count - 1)];
        }

        static Material BuildMaterial(string dir, string ch, string sourceName)
        {
            var sets = new List<int>();
            foreach (var f in Directory.GetFiles(dir, ch + "_*_Diffuse.png"))
                sets.Add(int.Parse(Path.GetFileName(f).Substring(ch.Length + 1, 4)));
            sets.Sort();
            int set = TextureSetFor(sourceName, sets);
            bool hair = sourceName.ToLowerInvariant().Contains("hair");
            string matPath = $"{dir}/{sourceName}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(AssetDatabase.LoadAssetAtPath<Material>(hair ? HairTemplate : BodyTemplate));
                AssetDatabase.CreateAsset(mat, matPath);
            }
            string p = $"{dir}/{ch}_{set}_";
            var diffuse = AssetDatabase.LoadAssetAtPath<Texture2D>(p + "Diffuse.png");
            mat.SetTexture("_BaseMap", diffuse); mat.SetTexture("_MainTex", diffuse);
            mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(p + "Normal.png"));
            var specGloss = BuildSpecGloss(p);
            mat.SetTexture("_SpecGlossMap", specGloss);
            if (specGloss) mat.EnableKeyword("_SPECGLOSSMAP"); else mat.DisableKeyword("_SPECGLOSSMAP");
            if (mat.GetTexture("_BumpMap")) mat.EnableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// Ghép Specular (RGB) + Glossiness (A) thành một texture như Ch31_1001_SpecGloss.png.
        static Texture2D BuildSpecGloss(string prefix)
        {
            string outPath = prefix + "SpecGloss.png";
            if (!File.Exists(outPath))
            {
                if (!File.Exists(prefix + "Specular.png") || !File.Exists(prefix + "Glossiness.png")) return null;
                var spec = new Texture2D(2, 2); spec.LoadImage(File.ReadAllBytes(prefix + "Specular.png"));
                var gloss = new Texture2D(2, 2); gloss.LoadImage(File.ReadAllBytes(prefix + "Glossiness.png"));
                if (gloss.width != spec.width || gloss.height != spec.height) gloss = Resize(gloss, spec.width, spec.height);
                var s = spec.GetPixels32(); var g = gloss.GetPixels32();
                for (int i = 0; i < s.Length; i++) s[i].a = g[i].r;
                var outTex = new Texture2D(spec.width, spec.height, TextureFormat.RGBA32, false);
                outTex.SetPixels32(s); File.WriteAllBytes(outPath, outTex.EncodeToPNG());
                Object.DestroyImmediate(spec); Object.DestroyImmediate(gloss); Object.DestroyImmediate(outTex);
                AssetDatabase.ImportAsset(outPath);
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(outPath);
        }

        static Texture2D Resize(Texture2D src, int w, int h)
        {
            var rt = RenderTexture.GetTemporary(w, h);
            Graphics.Blit(src, rt);
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false); t.ReadPixels(new Rect(0, 0, w, h), 0, 0); t.Apply();
            RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt);
            return t;
        }
    }
}
