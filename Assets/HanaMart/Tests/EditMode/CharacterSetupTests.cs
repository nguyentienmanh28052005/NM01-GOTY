using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HanaMart.Tests
{
    public class CharacterSetupTests
    {
        static readonly string[] Models =
        {
            "Assets/Art/Model/Charactor/Female Walk - Lat Mat.fbx",
            "Assets/Art/Model/Charactor/character (4).fbx",
            "Assets/Art/Model/Charactor/character (5).fbx",
            "Assets/Art/Model/Charactor/character (6).fbx",
        };

        [TestCaseSource(nameof(Models))]
        public void EveryRendererHasUrpLitWithBaseMap(string path)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(go, path);
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                foreach (var m in r.sharedMaterials)
                {
                    Assert.IsNotNull(m, r.name + " thiếu material");
                    Assert.AreEqual("Universal Render Pipeline/Lit", m.shader.name, m.name);
                    Assert.IsNotNull(m.GetTexture("_BaseMap"), m.name + " thiếu _BaseMap");
                }
        }

        [TestCaseSource(nameof(Models))]
        public void HairIsTransparent(string path)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                foreach (var m in r.sharedMaterials)
                    if (m != null && m.name.ToLowerInvariant().Contains("hair"))
                        Assert.AreEqual(1f, m.GetFloat("_Surface"), m.name + " phải Transparent");
        }

        [TestCaseSource(nameof(Models))]
        public void RigIsHumanoid(string path)
        {
            var imp = (ModelImporter)AssetImporter.GetAtPath(path);
            Assert.AreEqual(ModelImporterAnimationType.Human, imp.animationType, path);
            Avatar avatar = null;
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path)) if (o is Avatar a) avatar = a;
            Assert.IsNotNull(avatar, path + " không có Avatar");
            Assert.IsTrue(avatar.isValid && avatar.isHuman, path + " Avatar không hợp lệ");
        }
    }
}
