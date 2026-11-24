using System.IO;
using NUnit.Framework;
using UnityEditor;

namespace HanaMart.Tests
{
    public class BuildSettingsTests
    {
        [Test]
        public void AllBuildScenesExist()
        {
            foreach (var s in EditorBuildSettings.scenes)
                Assert.IsTrue(File.Exists(s.path), "Scene trong Build Settings không tồn tại: " + s.path);
        }

        [Test]
        public void HanaMartSceneIsFirst()
        {
            Assert.IsNotEmpty(EditorBuildSettings.scenes);
            Assert.AreEqual("Assets/HanaMart/Scenes/HanaMart24.unity", EditorBuildSettings.scenes[0].path);
        }
    }
}
