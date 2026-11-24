using HanaMart.EditorTools;
using NUnit.Framework;
using UnityEngine;

namespace HanaMart.Tests
{
    [Timeout(10000)]
    public class ClothingMaskTests
    {
        static readonly Color Skin=new Color(.886f,.725f,.596f),Blue=new Color(.05f,.2f,.6f);
        [Test] public void HeadAndHandsRemainSkin()
        {
            var head=ClothingMask.Region("mixamorig:Head",Skin);var hand=ClothingMask.Region("mixamorig:LeftHandIndex1",Skin);
            Assert.AreEqual(new Color(0,0,1,1),head);Assert.AreEqual(new Color(0,0,1,0),hand);
        }
        [Test] public void UpperBodyAndPantsUseSeparateChannels()
        {
            Assert.AreEqual(new Color(1,0,0,0),ClothingMask.Region("mixamorig:Spine2",Blue));
            Assert.AreEqual(new Color(0,1,0,0),ClothingMask.Region("mixamorig:LeftUpLeg",Blue));
        }
        [Test] public void BareArmsAreProtectedWhileSleevesAreTinted()
        {
            Assert.AreEqual(new Color(0,0,1,0),ClothingMask.Region("mixamorig:LeftForeArm",Skin));
            Assert.AreEqual(new Color(1,0,0,0),ClothingMask.Region("mixamorig:LeftForeArm",Blue));
        }
        [Test] public void ShoesDoNotReceiveClothingTint() => Assert.AreEqual(Color.clear,ClothingMask.Region("mixamorig:LeftFoot",Blue));
        [Test] public void NamedMeshesProtectBareLegsAndKeepPrintedClothing()
        {
            Assert.AreEqual(new Color(0,0,1,0),ClothingMask.Region("mixamorig:LeftUpLeg",Skin,"Ch42_Body1"));
            Assert.AreEqual(new Color(1,0,0,0),ClothingMask.Region("mixamorig:Spine2",Skin,"Ch08_Hoodie"));
            Assert.AreEqual(new Color(0,1,0,0),ClothingMask.Region("mixamorig:Hips",Skin,"Ch01_Pants"));
        }
    }
}
