using System;
using System.IO;
using HanaMart.UI;
using NUnit.Framework;

namespace HanaMart.Tests
{
    [Timeout(10000)]
    public class SettingsModelTests
    {
        string tempDir;

        [SetUp]
        public void SetUp()
        {
            tempDir = Path.Combine(Path.GetTempPath(), "HanaMartSettingsTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }

        [Test]
        public void PresetsMatchThreeJsDefinitions()
        {
            CollectionAssert.AreEqual(new[] { "low", "medium", "high", "ultra", "cine" }, GraphicsPresets.Keys);

            var low = GraphicsPresets.For("low");
            Assert.AreEqual("Thấp", low.Label);
            Assert.AreEqual(2, low.AreaLights);
            Assert.AreEqual(0.75f, low.RenderScaleCap, 1e-4f);
            Assert.IsFalse(low.Smaa);
            Assert.IsFalse(low.ExtraLights);

            var med = GraphicsPresets.For("medium");
            Assert.AreEqual("Trung bình", med.Label);
            Assert.AreEqual(4, med.AreaLights);
            Assert.AreEqual(1.0f, med.RenderScaleCap, 1e-4f);
            Assert.IsFalse(med.Smaa);

            var high = GraphicsPresets.For("high");
            Assert.AreEqual("Cao", high.Label);
            Assert.AreEqual(9, high.AreaLights);
            Assert.IsTrue(high.Smaa);
            Assert.IsTrue(high.ExtraLights);

            var ultra = GraphicsPresets.For("ultra");
            Assert.AreEqual("Siêu cao", ultra.Label);
            Assert.AreEqual(99, ultra.AreaLights);
            Assert.AreEqual(1.5f, ultra.RenderScaleCap, 1e-4f);

            var cine = GraphicsPresets.For("cine");
            Assert.AreEqual("Điện ảnh (quay video)", cine.Label);
            Assert.AreEqual(99, cine.AreaLights);
            Assert.AreEqual(2.0f, cine.RenderScaleCap, 1e-4f);
            Assert.IsTrue(cine.Dof);
            Assert.AreEqual(1f, cine.Cine, 1e-4f);

            Assert.AreEqual("Trung bình", GraphicsPresets.For("unknown").Label);
        }

        [Test]
        public void DynamicResolutionDropsUnderLowFpsAndRecovers()
        {
            var dyn = new DynamicResolutionController();
            Assert.AreEqual(1f, dyn.Scale, 1e-4f);

            // 30 FPS for 1.6s -> drops by 0.15 to 0.85
            for (int i = 0; i < 48; i++) dyn.Tick(1.6f / 48f, true);
            Assert.AreEqual(0.85f, dyn.Scale, 0.02f);

            // 45 FPS for 1.6s -> drops by 0.08 to ~0.77
            for (int i = 0; i < 72; i++) dyn.Tick(1.6f / 72f, true);
            Assert.AreEqual(0.77f, dyn.Scale, 0.02f);

            // Floor at 0.55
            for (int w = 0; w < 4; w++)
                for (int i = 0; i < 48; i++) dyn.Tick(1.6f / 48f, true);
            Assert.AreEqual(0.55f, dyn.Scale, 1e-4f);

            // 60 FPS recovers by 0.05
            for (int i = 0; i < 180; i++) dyn.Tick(1f / 60f, true);
            Assert.AreEqual(0.60f, dyn.Scale, 0.02f);

            // Disabling resets immediately to 1.0
            Assert.IsTrue(dyn.Tick(0.016f, false));
            Assert.AreEqual(1f, dyn.Scale, 1e-4f);
        }

        [Test]
        public void SettingsModelStepsAllThreeTabsAndClampsRanges()
        {
            var s = new GameSettings();
            Assert.AreEqual(13, SettingsModel.GetRows(SettingsTab.Gfx).Length);
            Assert.AreEqual(6, SettingsModel.GetRows(SettingsTab.Audio).Length);
            Assert.AreEqual(7, SettingsModel.GetRows(SettingsTab.Ctrl).Length);

            Assert.IsTrue(SettingsModel.StepRow(SettingsTab.Gfx, 0, 1, s));
            Assert.AreEqual("high", s.quality);
            Assert.IsTrue(SettingsModel.StepRow(SettingsTab.Gfx, 0, -2, s));
            Assert.AreEqual("low", s.quality);

            // FOV 60..100 step 2
            for (int i = 0; i < 30; i++) SettingsModel.StepRow(SettingsTab.Gfx, 3, 1, s);
            Assert.AreEqual(100, s.fov);
            for (int i = 0; i < 30; i++) SettingsModel.StepRow(SettingsTab.Gfx, 3, -1, s);
            Assert.AreEqual(60, s.fov);

            // Audio master 0..1 step 0.05
            for (int i = 0; i < 25; i++) SettingsModel.StepRow(SettingsTab.Audio, 0, 1, s);
            Assert.AreEqual(1f, s.master, 1e-4f);
            SettingsModel.StepRow(SettingsTab.Audio, 3, 1, s);
            Assert.IsFalse(s.subtitles);

            // Controls guide, invertY, rumble
            SettingsModel.StepRow(SettingsTab.Ctrl, 2, 1, s);
            Assert.IsTrue(s.guide);
            SettingsModel.StepRow(SettingsTab.Ctrl, 3, 1, s);
            Assert.IsTrue(s.invertY);
            SettingsModel.StepRow(SettingsTab.Ctrl, 4, 1, s);
            Assert.IsFalse(s.rumble);
        }

        [Test]
        public void SettingsPersistAcrossSaveReloadAndResetRestoresDefaults()
        {
            string path = Path.Combine(tempDir, SaveStore.FileName);
            var data = SaveData.CreateDefault();
            data.Settings.quality = "cine";
            data.Settings.fov = 90;
            data.Settings.master = 0.55f;
            data.Settings.amb = 0.45f;
            data.Settings.sfx = 0.65f;
            data.Settings.subtitles = false;
            data.Settings.guide = true;
            data.Settings.invertY = true;
            data.Settings.smoothCam = true;
            data.Settings.recMode = true;

            Assert.IsTrue(SaveStore.Save(data, path));
            var reloaded = SaveStore.Load(path);
            Assert.AreEqual("cine", reloaded.Settings.quality);
            Assert.AreEqual(90, reloaded.Settings.fov);
            Assert.AreEqual(0.55f, reloaded.Settings.master, 1e-4f);
            Assert.AreEqual(0.45f, reloaded.Settings.amb, 1e-4f);
            Assert.AreEqual(0.65f, reloaded.Settings.sfx, 1e-4f);
            Assert.IsFalse(reloaded.Settings.subtitles);
            Assert.IsTrue(reloaded.Settings.guide);
            Assert.IsTrue(reloaded.Settings.invertY);
            Assert.IsTrue(reloaded.Settings.smoothCam);
            Assert.IsTrue(reloaded.Settings.recMode);

            SettingsModel.ResetDefaults(reloaded.Settings);
            Assert.IsTrue(SaveStore.Save(reloaded, path));
            var afterReset = SaveStore.Load(path);
            Assert.AreEqual("medium", afterReset.Settings.quality);
            Assert.AreEqual(72, afterReset.Settings.fov);
            Assert.AreEqual(0.9f, afterReset.Settings.master, 1e-4f);
            Assert.AreEqual(0.8f, afterReset.Settings.amb, 1e-4f);
            Assert.AreEqual(0.9f, afterReset.Settings.sfx, 1e-4f);
            Assert.IsTrue(afterReset.Settings.subtitles);
            Assert.IsFalse(afterReset.Settings.guide);
            Assert.IsFalse(afterReset.Settings.invertY);
        }

        [Test]
        public void EffectiveRenderScaleRespectsPresetRenderScaleAndDynScale()
        {
            Assert.AreEqual(0.75f, SettingsModel.ComputeEffectiveScale("low", 1f, 1f), 1e-4f);
            Assert.AreEqual(0.5f, SettingsModel.ComputeEffectiveScale("low", 0.5f, 0.6f), 1e-4f);
            Assert.AreEqual(1.5f, SettingsModel.ComputeEffectiveScale("ultra", 1f, 1f), 1e-4f);
            Assert.AreEqual(2f, SettingsModel.ComputeEffectiveScale("cine", 1f, 1f), 1e-4f);
        }

        [Test]
        public void PerfOverlayFormatsFpsResolutionScaleAndPreset()
        {
            Assert.AreEqual("60 FPS · 1280×720 · độ phân giải ×0.85 · Trung bình",
                PerfOverlayModel.Format(60, 1280, 720, 0.85f, "Trung bình"));
        }
    }
}
