using System;
using System.IO;
using HanaMart.UI;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace HanaMart.Tests
{
    [Timeout(10000)]
    public class SaveDataTests
    {
        string tempDir;

        [SetUp]
        public void SetUp()
        {
            tempDir = Path.Combine(Path.GetTempPath(), "HanaMartSaveTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }

        [Test]
        public void DefaultsMatchThreeJsHana24V3()
        {
            var data = SaveData.CreateDefault();
            var s = data.Settings;
            Assert.IsFalse(s.guide);
            Assert.AreEqual("medium", s.quality);
            Assert.IsTrue(s.dynRes);
            Assert.AreEqual(1f, s.renderScale, 1e-4f);
            Assert.AreEqual(72, s.fov);
            Assert.AreEqual(1f, s.mouseSens, 1e-4f);
            Assert.AreEqual(1f, s.padSens, 1e-4f);
            Assert.IsFalse(s.invertY);
            Assert.IsTrue(s.headBob);
            Assert.IsTrue(s.grain);
            Assert.AreEqual(0.9f, s.master, 1e-4f);
            Assert.AreEqual(0.8f, s.amb, 1e-4f);
            Assert.AreEqual(0.9f, s.sfx, 1e-4f);
            Assert.AreEqual(1f, s.brightness, 1e-4f);
            Assert.IsTrue(s.rumble);
            Assert.IsTrue(s.subtitles);
            Assert.IsFalse(s.cineFx);
            Assert.IsFalse(s.recMode);
            Assert.IsFalse(s.smoothCam);
            Assert.IsFalse(s.autoTuned);
            Assert.IsFalse(data.TutSeen);
            Assert.AreEqual(0, data.UnlockedEndingsCount);
        }

        [Test]
        public void LoadMergesMissingDefaultsAndPreservesNewFields()
        {
            string path = Path.Combine(tempDir, SaveStore.FileName);
            File.WriteAllText(path,
                "{\"settings\":{\"fov\":84,\"futureOption\":\"keep\"},\"endings\":{\"A\":1},\"futureRoot\":42,\"best\":{\"fastest\":120}}");

            var data = SaveStore.Load(path);
            Assert.AreEqual(84, data.Settings.fov);
            Assert.AreEqual("medium", data.Settings.quality);
            Assert.AreEqual(0.9f, data.Settings.master, 1e-4f);
            Assert.IsTrue(data.Settings.subtitles);
            Assert.IsTrue(data.Settings.dynRes);
            Assert.IsTrue(data.Settings.headBob);
            Assert.IsTrue(data.HasEnding("A"));
            Assert.IsFalse(data.HasEnding("B"));
            Assert.AreEqual(1, data.UnlockedEndingsCount);

            Assert.IsTrue(SaveStore.Save(data, path));
            var raw = JObject.Parse(File.ReadAllText(path));
            Assert.AreEqual("keep", (string)raw["settings"]["futureOption"]);
            Assert.AreEqual(42, (int)raw["futureRoot"]);
            Assert.AreEqual(120, (int)raw["best"]["fastest"]);
        }

        [Test]
        public void LoadCorruptedOrInvalidJsonFallsBackToDefaults()
        {
            string path = Path.Combine(tempDir, "broken.json");
            foreach (string bad in new[] { "{broken", "", "[1,2,3]", "null" })
            {
                File.WriteAllText(path, bad);
                var loaded = SaveStore.Load(path);
                Assert.AreEqual("medium", loaded.Settings.quality);
                Assert.AreEqual(72, loaded.Settings.fov);
                Assert.AreEqual(0.9f, loaded.Settings.master, 1e-4f);
                Assert.AreEqual(0, loaded.UnlockedEndingsCount);
            }

            File.WriteAllText(path, "{\"settings\":{\"fov\":\"bad\",\"master\":true,\"quality\":123},\"endings\":\"bad\"}");
            var wrongTypes = SaveStore.Load(path);
            Assert.AreEqual(72, wrongTypes.Settings.fov);
            Assert.AreEqual(0.9f, wrongTypes.Settings.master, 1e-4f);
            Assert.AreEqual("medium", wrongTypes.Settings.quality);
            Assert.AreEqual(0, wrongTypes.UnlockedEndingsCount);

            File.WriteAllText(path, "{\"settings\":{\"fov\":\"bad\",\"grain\":false}}");
            var partialSibling = SaveStore.Load(path);
            Assert.AreEqual(72, partialSibling.Settings.fov);
            Assert.IsFalse(partialSibling.Settings.grain);
        }

        [Test]
        public void SaveReplacesFileSafelyWithoutLeavingTempFile()
        {
            Assert.AreEqual("hana24_v3.json", SaveStore.FileName);
            string nestedDir = Path.Combine(tempDir, "sub", "dir");
            string path = Path.Combine(nestedDir, SaveStore.FileName);

            var data = SaveData.CreateDefault();
            data.Settings.fov = 90;
            Assert.IsTrue(SaveStore.Save(data, path));

            data.Settings.fov = 66;
            data.TutSeen = true;
            Assert.IsTrue(SaveStore.Save(data, path));

            var reloaded = SaveStore.Load(path);
            Assert.AreEqual(66, reloaded.Settings.fov);
            Assert.IsTrue(reloaded.TutSeen);

            string[] files = Directory.GetFiles(nestedDir);
            Assert.AreEqual(1, files.Length);
            Assert.AreEqual(SaveStore.FileName, Path.GetFileName(files[0]));
        }

        [Test]
        public void RecordsBothEndingsAndCountsUnlocked()
        {
            string path = Path.Combine(tempDir, SaveStore.FileName);
            var data = SaveData.CreateDefault();

            Assert.IsTrue(data.RecordEnding("A"));
            Assert.AreEqual(1, data.UnlockedEndingsCount);
            Assert.IsFalse(data.RecordEnding("A"));
            Assert.AreEqual(1, data.UnlockedEndingsCount);

            Assert.IsTrue(data.RecordEnding("B"));
            Assert.AreEqual(2, data.UnlockedEndingsCount);

            Assert.IsFalse(data.RecordEnding(""));
            Assert.IsFalse(data.RecordEnding("C"));
            Assert.AreEqual(2, data.UnlockedEndingsCount);

            Assert.IsTrue(SaveStore.Save(data, path));
            var reloaded = SaveStore.Load(path);
            Assert.IsTrue(reloaded.HasEnding("A"));
            Assert.IsTrue(reloaded.HasEnding("B"));
            Assert.AreEqual(2, reloaded.UnlockedEndingsCount);
        }
    }
}
