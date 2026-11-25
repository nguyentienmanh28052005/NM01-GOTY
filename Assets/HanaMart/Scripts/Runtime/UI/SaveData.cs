using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace HanaMart.UI
{
    public sealed class GameSettings
    {
        public bool guide;
        public string quality = "medium";
        public bool dynRes = true;
        public float renderScale = 1f;
        public int fov = 72;
        public float mouseSens = 1f;
        public float padSens = 1f;
        public bool invertY;
        public bool headBob = true;
        public bool grain = true;
        public float master = 0.9f;
        public float amb = 0.8f;
        public float sfx = 0.9f;
        public float brightness = 1f;
        public bool rumble = true;
        public bool subtitles = true;
        public bool cineFx;
        public bool recMode;
        public bool smoothCam;
        public bool autoTuned;

        public GameSettings Clone() => (GameSettings)MemberwiseClone();
    }

    public sealed class SaveData
    {
        public GameSettings Settings { get; } = new();
        public HashSet<string> Endings { get; } = new(StringComparer.Ordinal);
        public bool TutSeen { get; set; }

        internal JObject ExtraRoot { get; set; } = new();
        internal JObject ExtraSettings { get; set; } = new();

        public int UnlockedEndingsCount => (Endings.Contains("A") ? 1 : 0) + (Endings.Contains("B") ? 1 : 0);
        public bool HasEnding(string id) => !string.IsNullOrEmpty(id) && Endings.Contains(id);

        public bool RecordEnding(string id)
        {
            if (id != "A" && id != "B") return false;
            return Endings.Add(id);
        }

        public static SaveData CreateDefault() => new();
    }

    public static class SaveStore
    {
        public const string FileName = "hana24_v3.json";
        public static string DefaultPath => Path.Combine(Application.persistentDataPath, FileName);

        static readonly HashSet<string> ValidQualities = new(StringComparer.Ordinal)
        {
            "low", "medium", "high", "ultra", "cine"
        };

        static readonly HashSet<string> KnownRootKeys = new(StringComparer.Ordinal)
        {
            "settings", "endings", "tutSeen"
        };

        static readonly HashSet<string> KnownSettingKeys = new(StringComparer.Ordinal)
        {
            "guide", "quality", "dynRes", "renderScale", "fov", "mouseSens", "padSens",
            "invertY", "headBob", "grain", "master", "amb", "sfx", "brightness",
            "rumble", "subtitles", "cineFx", "recMode", "smoothCam", "autoTuned"
        };

        public static SaveData Load(string path = null)
        {
            string target = string.IsNullOrEmpty(path) ? DefaultPath : path;
            var data = SaveData.CreateDefault();
            if (!File.Exists(target)) return data;

            try
            {
                string json = File.ReadAllText(target, Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(json)) return data;
                var token = JToken.Parse(json);
                if (token is not JObject root) return data;

                foreach (var prop in root.Properties())
                {
                    if (!KnownRootKeys.Contains(prop.Name))
                        data.ExtraRoot[prop.Name] = prop.Value.DeepClone();
                }

                if (root["tutSeen"] is JValue tutVal && TryBool(tutVal, out bool tut))
                    data.TutSeen = tut;

                if (root["endings"] is JObject endingsObj)
                {
                    foreach (var prop in endingsObj.Properties())
                    {
                        if ((prop.Name == "A" || prop.Name == "B") &&
                            prop.Value is JValue v && TryBool(v, out bool unlocked) && unlocked)
                        {
                            data.Endings.Add(prop.Name);
                        }
                    }
                }

                if (root["settings"] is JObject settingsObj)
                {
                    foreach (var prop in settingsObj.Properties())
                    {
                        if (!KnownSettingKeys.Contains(prop.Name))
                            data.ExtraSettings[prop.Name] = prop.Value.DeepClone();
                    }

                    var s = data.Settings;
                    ReadBool(settingsObj, "guide", ref s.guide);
                    if (TryString(settingsObj["quality"], out string q) && ValidQualities.Contains(q))
                        s.quality = q;
                    ReadBool(settingsObj, "dynRes", ref s.dynRes);
                    ReadFloat(settingsObj, "renderScale", ref s.renderScale, 0.5f, 1f);
                    ReadInt(settingsObj, "fov", ref s.fov, 60, 100);
                    ReadFloat(settingsObj, "mouseSens", ref s.mouseSens, 0.2f, 3f);
                    ReadFloat(settingsObj, "padSens", ref s.padSens, 0.3f, 2.5f);
                    ReadBool(settingsObj, "invertY", ref s.invertY);
                    ReadBool(settingsObj, "headBob", ref s.headBob);
                    ReadBool(settingsObj, "grain", ref s.grain);
                    ReadFloat(settingsObj, "master", ref s.master, 0f, 1f);
                    ReadFloat(settingsObj, "amb", ref s.amb, 0f, 1f);
                    ReadFloat(settingsObj, "sfx", ref s.sfx, 0f, 1f);
                    ReadFloat(settingsObj, "brightness", ref s.brightness, 0.7f, 1.4f);
                    ReadBool(settingsObj, "rumble", ref s.rumble);
                    ReadBool(settingsObj, "subtitles", ref s.subtitles);
                    ReadBool(settingsObj, "cineFx", ref s.cineFx);
                    ReadBool(settingsObj, "recMode", ref s.recMode);
                    ReadBool(settingsObj, "smoothCam", ref s.smoothCam);
                    ReadBool(settingsObj, "autoTuned", ref s.autoTuned);
                }

                return data;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is JsonException)
            {
                return SaveData.CreateDefault();
            }
        }

        public static bool Save(SaveData data, string path = null)
        {
            if (data == null) return false;
            string target = string.IsNullOrEmpty(path) ? DefaultPath : path;
            string tempPath = target + ".tmp";
            string backupPath = target + ".bak";

            try
            {
                string dir = Path.GetDirectoryName(target);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                var root = data.ExtraRoot != null ? (JObject)data.ExtraRoot.DeepClone() : new JObject();
                if (root["best"] == null || root["best"].Type != JTokenType.Object)
                    root["best"] = new JObject();

                var settingsObj = data.ExtraSettings != null ? (JObject)data.ExtraSettings.DeepClone() : new JObject();
                var s = data.Settings;
                settingsObj["guide"] = s.guide;
                settingsObj["quality"] = ValidQualities.Contains(s.quality) ? s.quality : "medium";
                settingsObj["dynRes"] = s.dynRes;
                settingsObj["renderScale"] = s.renderScale;
                settingsObj["fov"] = s.fov;
                settingsObj["mouseSens"] = s.mouseSens;
                settingsObj["padSens"] = s.padSens;
                settingsObj["invertY"] = s.invertY;
                settingsObj["headBob"] = s.headBob;
                settingsObj["grain"] = s.grain;
                settingsObj["master"] = s.master;
                settingsObj["amb"] = s.amb;
                settingsObj["sfx"] = s.sfx;
                settingsObj["brightness"] = s.brightness;
                settingsObj["rumble"] = s.rumble;
                settingsObj["subtitles"] = s.subtitles;
                settingsObj["cineFx"] = s.cineFx;
                settingsObj["recMode"] = s.recMode;
                settingsObj["smoothCam"] = s.smoothCam;
                settingsObj["autoTuned"] = s.autoTuned;
                root["settings"] = settingsObj;

                var endingsObj = new JObject();
                if (data.HasEnding("A")) endingsObj["A"] = 1;
                if (data.HasEnding("B")) endingsObj["B"] = 1;
                root["endings"] = endingsObj;

                if (data.TutSeen) root["tutSeen"] = 1;
                else root.Remove("tutSeen");

                byte[] bytes = Encoding.UTF8.GetBytes(root.ToString(Formatting.Indented));
                using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    fs.Write(bytes, 0, bytes.Length);
                    fs.Flush(true);
                }

                if (File.Exists(target))
                {
                    try
                    {
                        File.Replace(tempPath, target, backupPath, true);
                        if (File.Exists(backupPath)) File.Delete(backupPath);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        if (File.Exists(target)) File.Delete(target);
                        File.Move(tempPath, target);
                    }
                    catch (IOException)
                    {
                        if (File.Exists(target)) File.Delete(target);
                        File.Move(tempPath, target);
                    }
                }
                else
                {
                    File.Move(tempPath, target);
                }

                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                return false;
            }
            finally
            {
                try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
                try { if (File.Exists(backupPath)) File.Delete(backupPath); } catch { }
            }
        }

        static void ReadBool(JObject obj, string key, ref bool target)
        {
            if (obj[key] is JValue v && TryBool(v, out bool value)) target = value;
        }

        static void ReadInt(JObject obj, string key, ref int target, int min, int max)
        {
            if (obj[key] is JValue v && (v.Type == JTokenType.Integer || v.Type == JTokenType.Float))
            {
                int val = Convert.ToInt32(v.Value);
                if (val >= min && val <= max) target = val;
            }
        }

        static void ReadFloat(JObject obj, string key, ref float target, float min, float max)
        {
            if (obj[key] is JValue v && (v.Type == JTokenType.Integer || v.Type == JTokenType.Float))
            {
                float val = Convert.ToSingle(v.Value);
                if (!float.IsNaN(val) && !float.IsInfinity(val))
                    target = Mathf.Clamp(val, min, max);
            }
        }

        static bool TryString(JToken token, out string value)
        {
            if (token is JValue v && v.Type == JTokenType.String)
            {
                value = (string)v.Value;
                return true;
            }
            value = null;
            return false;
        }

        static bool TryBool(JValue v, out bool value)
        {
            if (v.Type == JTokenType.Boolean) { value = (bool)v.Value; return true; }
            if (v.Type == JTokenType.Integer) { value = Convert.ToInt64(v.Value) != 0; return true; }
            value = false;
            return false;
        }
    }
}
