using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace HanaMart.UI
{
    public enum SettingsTab { Gfx, Audio, Ctrl }
    public enum SettingsRowKind { Enum, Range, Bool, Fullscreen, Reset, Back }

    public sealed class GraphicsPreset
    {
        public string Key, Label;
        public int AreaLights, Aniso;
        public float RenderScaleCap, Cine;
        public bool Smaa, ExtraLights, Dof;
    }

    public static class GraphicsPresets
    {
        public static readonly string[] Keys = { "low", "medium", "high", "ultra", "cine" };

        static readonly Dictionary<string, GraphicsPreset> Map = new(StringComparer.Ordinal)
        {
            ["low"] = new GraphicsPreset { Key = "low", Label = "Thấp", AreaLights = 2, RenderScaleCap = 0.75f, Aniso = 4 },
            ["medium"] = new GraphicsPreset { Key = "medium", Label = "Trung bình", AreaLights = 4, RenderScaleCap = 1.0f, Aniso = 8 },
            ["high"] = new GraphicsPreset { Key = "high", Label = "Cao", AreaLights = 9, RenderScaleCap = 1.0f, Smaa = true, Aniso = 16, ExtraLights = true },
            ["ultra"] = new GraphicsPreset { Key = "ultra", Label = "Siêu cao", AreaLights = 99, RenderScaleCap = 1.5f, Smaa = true, Aniso = 16, ExtraLights = true },
            ["cine"] = new GraphicsPreset { Key = "cine", Label = "Điện ảnh (quay video)", AreaLights = 99, RenderScaleCap = 2.0f, Smaa = true, Aniso = 16, ExtraLights = true, Dof = true, Cine = 1f },
        };

        public static GraphicsPreset For(string key) =>
            !string.IsNullOrEmpty(key) && Map.TryGetValue(key, out var preset) ? preset : Map["medium"];
    }

    public sealed class DynamicResolutionController
    {
        float accTime;
        int frames;
        public float Scale { get; private set; } = 1f;

        public void Reset()
        {
            accTime = 0f;
            frames = 0;
            Scale = 1f;
        }

        public bool Tick(float realDt, bool enabled)
        {
            if (!enabled)
            {
                if (Mathf.Abs(Scale - 1f) > 1e-4f)
                {
                    Reset();
                    return true;
                }
                accTime = 0f;
                frames = 0;
                return false;
            }

            accTime += Mathf.Max(0f, realDt);
            frames++;
            if (accTime < 1.5f) return false;

            float fps = frames / accTime;
            accTime = 0f;
            frames = 0;

            float next = Scale;
            if (fps < 50f) next = Mathf.Max(0.55f, Scale - (fps < 35f ? 0.15f : 0.08f));
            else if (fps > 58f && Scale < 1f) next = Mathf.Min(1f, Scale + 0.05f);

            if (Mathf.Abs(next - Scale) > 0.01f)
            {
                Scale = Mathf.Round(next * 100f) / 100f;
                return true;
            }
            return false;
        }
    }

    public sealed class SettingsRowSpec
    {
        public string Label, Key;
        public SettingsRowKind Kind;
    }

    public static class SettingsModel
    {
        static readonly SettingsRowSpec[] GfxRows =
        {
            new() { Key = "quality", Label = "Chất lượng đồ họa", Kind = SettingsRowKind.Enum },
            new() { Key = "renderScale", Label = "Độ phân giải kết xuất", Kind = SettingsRowKind.Range },
            new() { Key = "dynRes", Label = "Độ phân giải động (chống giật)", Kind = SettingsRowKind.Bool },
            new() { Key = "fov", Label = "Góc nhìn (FOV)", Kind = SettingsRowKind.Range },
            new() { Key = "brightness", Label = "Độ sáng", Kind = SettingsRowKind.Range },
            new() { Key = "grain", Label = "Hạt phim", Kind = SettingsRowKind.Bool },
            new() { Key = "headBob", Label = "Lắc đầu khi đi", Kind = SettingsRowKind.Bool },
            new() { Key = "cineFx", Label = "Hiệu ứng điện ảnh (vệt sáng, quầng phim)", Kind = SettingsRowKind.Bool },
            new() { Key = "recMode", Label = "Chế độ quay video (ẩn giao diện · phím H)", Kind = SettingsRowKind.Bool },
            new() { Key = "smoothCam", Label = "Camera mượt (cho quay video)", Kind = SettingsRowKind.Bool },
            new() { Key = "fullscreen", Label = "Toàn màn hình", Kind = SettingsRowKind.Fullscreen },
            new() { Key = "reset", Label = "Đặt lại mặc định", Kind = SettingsRowKind.Reset },
            new() { Key = "back", Label = "← Quay lại", Kind = SettingsRowKind.Back },
        };

        static readonly SettingsRowSpec[] AudioRows =
        {
            new() { Key = "master", Label = "Âm lượng tổng", Kind = SettingsRowKind.Range },
            new() { Key = "amb", Label = "Môi trường", Kind = SettingsRowKind.Range },
            new() { Key = "sfx", Label = "Hiệu ứng", Kind = SettingsRowKind.Range },
            new() { Key = "subtitles", Label = "Phụ đề", Kind = SettingsRowKind.Bool },
            new() { Key = "reset", Label = "Đặt lại mặc định", Kind = SettingsRowKind.Reset },
            new() { Key = "back", Label = "← Quay lại", Kind = SettingsRowKind.Back },
        };

        static readonly SettingsRowSpec[] CtrlRows =
        {
            new() { Key = "mouseSens", Label = "Độ nhạy chuột", Kind = SettingsRowKind.Range },
            new() { Key = "padSens", Label = "Độ nhạy tay cầm", Kind = SettingsRowKind.Range },
            new() { Key = "guide", Label = "Gợi ý nhiệm vụ (dấu ◆ chỉ đường)", Kind = SettingsRowKind.Bool },
            new() { Key = "invertY", Label = "Đảo trục dọc", Kind = SettingsRowKind.Bool },
            new() { Key = "rumble", Label = "Rung tay cầm", Kind = SettingsRowKind.Bool },
            new() { Key = "reset", Label = "Đặt lại mặc định", Kind = SettingsRowKind.Reset },
            new() { Key = "back", Label = "← Quay lại", Kind = SettingsRowKind.Back },
        };

        public static SettingsRowSpec[] GetRows(SettingsTab tab) => tab switch
        {
            SettingsTab.Audio => AudioRows,
            SettingsTab.Ctrl => CtrlRows,
            _ => GfxRows,
        };

        public static bool StepRow(SettingsTab tab, int rowIndex, int delta, GameSettings s)
        {
            var rows = GetRows(tab);
            if (s == null || delta == 0 || rowIndex < 0 || rowIndex >= rows.Length) return false;
            string key = rows[rowIndex].Key;
            switch (key)
            {
                case "quality":
                    int idx = Array.IndexOf(GraphicsPresets.Keys, s.quality);
                    if (idx < 0) idx = 1;
                    int nextIdx = ((idx + delta) % GraphicsPresets.Keys.Length + GraphicsPresets.Keys.Length) % GraphicsPresets.Keys.Length;
                    s.quality = GraphicsPresets.Keys[nextIdx];
                    s.autoTuned = true;
                    return true;
                case "renderScale": return StepFloat(ref s.renderScale, 0.5f, 1.0f, 0.05f, delta);
                case "dynRes": s.dynRes = !s.dynRes; return true;
                case "fov": return StepInt(ref s.fov, 60, 100, 2, delta);
                case "brightness": return StepFloat(ref s.brightness, 0.7f, 1.4f, 0.05f, delta);
                case "grain": s.grain = !s.grain; return true;
                case "headBob": s.headBob = !s.headBob; return true;
                case "cineFx": s.cineFx = !s.cineFx; return true;
                case "recMode": s.recMode = !s.recMode; return true;
                case "smoothCam": s.smoothCam = !s.smoothCam; return true;
                case "master": return StepFloat(ref s.master, 0f, 1f, 0.05f, delta);
                case "amb": return StepFloat(ref s.amb, 0f, 1f, 0.05f, delta);
                case "sfx": return StepFloat(ref s.sfx, 0f, 1f, 0.05f, delta);
                case "subtitles": s.subtitles = !s.subtitles; return true;
                case "mouseSens": return StepFloat(ref s.mouseSens, 0.2f, 3.0f, 0.1f, delta);
                case "padSens": return StepFloat(ref s.padSens, 0.3f, 2.5f, 0.1f, delta);
                case "guide": s.guide = !s.guide; return true;
                case "invertY": s.invertY = !s.invertY; return true;
                case "rumble": s.rumble = !s.rumble; return true;
                default: return false;
            }
        }

        public static string FormatValue(SettingsTab tab, int rowIndex, GameSettings s)
        {
            var rows = GetRows(tab);
            if (s == null || rowIndex < 0 || rowIndex >= rows.Length) return "";
            return rows[rowIndex].Key switch
            {
                "quality" => GraphicsPresets.For(s.quality).Label,
                "renderScale" => Mathf.RoundToInt(s.renderScale * 100f) + "%",
                "dynRes" => OnOff(s.dynRes),
                "fov" => s.fov + "°",
                "brightness" => Mathf.RoundToInt(s.brightness * 100f) + "%",
                "grain" => OnOff(s.grain),
                "headBob" => OnOff(s.headBob),
                "cineFx" => OnOff(s.cineFx),
                "recMode" => OnOff(s.recMode),
                "smoothCam" => OnOff(s.smoothCam),
                "fullscreen" => "F11",
                "master" => Mathf.RoundToInt(s.master * 100f).ToString(),
                "amb" => Mathf.RoundToInt(s.amb * 100f).ToString(),
                "sfx" => Mathf.RoundToInt(s.sfx * 100f).ToString(),
                "subtitles" => OnOff(s.subtitles),
                "mouseSens" => s.mouseSens.ToString("0.0", CultureInfo.InvariantCulture),
                "padSens" => s.padSens.ToString("0.0", CultureInfo.InvariantCulture),
                "guide" => OnOff(s.guide),
                "invertY" => OnOff(s.invertY),
                "rumble" => OnOff(s.rumble),
                _ => "",
            };
        }

        public static float NormalizedFill(SettingsTab tab, int rowIndex, GameSettings s)
        {
            var rows = GetRows(tab);
            if (s == null || rowIndex < 0 || rowIndex >= rows.Length) return -1f;
            return rows[rowIndex].Key switch
            {
                "renderScale" => Mathf.InverseLerp(0.5f, 1.0f, s.renderScale),
                "fov" => Mathf.InverseLerp(60f, 100f, s.fov),
                "brightness" => Mathf.InverseLerp(0.7f, 1.4f, s.brightness),
                "master" => Mathf.InverseLerp(0f, 1f, s.master),
                "amb" => Mathf.InverseLerp(0f, 1f, s.amb),
                "sfx" => Mathf.InverseLerp(0f, 1f, s.sfx),
                "mouseSens" => Mathf.InverseLerp(0.2f, 3.0f, s.mouseSens),
                "padSens" => Mathf.InverseLerp(0.3f, 2.5f, s.padSens),
                _ => -1f,
            };
        }

        public static void ResetDefaults(GameSettings s)
        {
            if (s == null) return;
            var d = new GameSettings();
            s.guide = d.guide;
            s.quality = d.quality;
            s.dynRes = d.dynRes;
            s.renderScale = d.renderScale;
            s.fov = d.fov;
            s.mouseSens = d.mouseSens;
            s.padSens = d.padSens;
            s.invertY = d.invertY;
            s.headBob = d.headBob;
            s.grain = d.grain;
            s.master = d.master;
            s.amb = d.amb;
            s.sfx = d.sfx;
            s.brightness = d.brightness;
            s.rumble = d.rumble;
            s.subtitles = d.subtitles;
            s.cineFx = d.cineFx;
            s.recMode = d.recMode;
            s.smoothCam = d.smoothCam;
            s.autoTuned = d.autoTuned;
        }

        public static float ComputeEffectiveScale(string quality, float renderScale, float dynScale)
        {
            float cap = GraphicsPresets.For(quality).RenderScaleCap;
            return Mathf.Clamp(cap * renderScale * dynScale, 0.5f, 2.0f);
        }

        static string OnOff(bool v) => v ? "Bật" : "Tắt";

        static bool StepFloat(ref float val, float min, float max, float step, int delta)
        {
            float next = Mathf.Round(Mathf.Clamp(val + delta * step, min, max) * 1000f) / 1000f;
            if (Mathf.Approximately(next, val)) return false;
            val = next;
            return true;
        }

        static bool StepInt(ref int val, int min, int max, int step, int delta)
        {
            int next = Mathf.Clamp(val + delta * step, min, max);
            if (next == val) return false;
            val = next;
            return true;
        }
    }

    public static class PerfOverlayModel
    {
        public static string Format(int fps, int width, int height, float scale, string presetLabel) =>
            $"{fps} FPS · {width}×{height} · độ phân giải ×{scale.ToString("0.00", CultureInfo.InvariantCulture)} · {presetLabel}";
    }
}
