using HanaMart.Actors;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

namespace HanaMart.Tests
{
    [Timeout(10000)]
    public class ActorMaterialTests
    {
        [Test,Timeout(60000)]
        public void ForwardShadowDepthAndNormalPassesCompileOnCurrentApi()
        {
            var entry=AssetDatabase.LoadAssetAtPath<ActorCatalog>("Assets/HanaMart/Data/ActorCatalog.asset").models[0];
            var body=entry.renderers.SelectMany(s=>s.materials).First(m=>m.shader.name=="HanaMart/Character");
            var rain=entry.renderers.SelectMany(s=>s.rainMaterials).First(m=>m.shader.name=="HanaMart/Character");
            foreach(var material in new[]{body,rain})
            {
                Assert.AreEqual(4,material.passCount);
                for(int pass=0;pass<4;pass++)
                {
                    ShaderUtil.CompilePass(material,pass,true);
                    Assert.IsFalse(ShaderUtil.ShaderHasError(material.shader),string.Join("\n",ShaderUtil.GetShaderMessages(material.shader).Select(m=>m.message)));
                }
            }
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void BakedMasksContainSkinFaceAndClothing(int index)
        {
            var actor=AssetDatabase.LoadAssetAtPath<ActorCatalog>("Assets/HanaMart/Data/ActorCatalog.asset").models[index];
            int clothing=0,skin=0,face=0;
            Assert.LessOrEqual(actor.renderers.Length,16);
            foreach(var slot in actor.renderers)foreach(var material in slot.materials)
            {
                if(!material.HasProperty("_RegionMask"))continue;
                var mask=material.GetTexture("_RegionMask");var image=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
                try
                {
                    ImageConversion.LoadImage(image,File.ReadAllBytes(AssetDatabase.GetAssetPath(mask)));
                    Assert.LessOrEqual(image.width*image.height,2048*2048);
                    foreach(var pixel in image.GetPixels32()){if(pixel.r>127 || pixel.g>127)clothing++;if(pixel.b>127)skin++;if(pixel.a>127)face++;}
                }
                finally{Object.DestroyImmediate(image);}
            }
            Assert.Greater(clothing,100,actor.id+" clothing");Assert.Greater(skin,100,actor.id+" skin");Assert.Greater(face,100,actor.id+" face");
        }
        [Test] public void BodyMaterialsKeepMapsAndHaveLinearMasksAndRainShells()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<ActorCatalog>("Assets/HanaMart/Data/ActorCatalog.asset");
            foreach(var actor in catalog.models)
            {
                Assert.Greater(actor.renderers.Length,0,actor.id);
                int bodies=0;
                foreach(var slot in actor.renderers)
                {
                    Assert.AreEqual(slot.materials.Length,slot.rainMaterials.Length,slot.path);
                    for(int i=0;i<slot.materials.Length;i++)
                    {
                        var material=slot.materials[i];Assert.IsNotNull(material,slot.path);
                        if(material.name.ToLowerInvariant().Contains("hair"))continue;
                        bodies++;Assert.AreEqual("HanaMart/Character",material.shader.name,material.name);
                        Assert.IsFalse(ShaderUtil.ShaderHasError(material.shader),material.name);
                        Assert.IsNotNull(material.GetTexture("_BaseMap"),material.name);Assert.IsNotNull(material.GetTexture("_BumpMap"),material.name);
                        var mask=material.GetTexture("_RegionMask");Assert.IsNotNull(mask,material.name);
                        Assert.IsFalse(((TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(mask))).sRGBTexture,material.name);
                        Assert.IsNotNull(slot.rainMaterials[i],material.name);Assert.AreEqual(1,slot.rainMaterials[i].GetFloat("_Raincoat"));
                    }
                }
                Assert.Greater(bodies,0,actor.id);
            }
        }
    }
}
