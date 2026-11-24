using System.IO;
using HanaMart.Render;
using HanaMart.Store;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace HanaMart.EditorTools
{
    /// Chụp scene HanaMart24 tại các góc của tools/three-export/viewpoints.json (tọa độ Three.js) → docs/visual/unity/<name>.png.
    public static class ViewpointCapture
    {
        const string ViewpointsPath = "tools/three-export/viewpoints.json";
        const string OutDir = "docs/visual/unity";

        [MenuItem("HanaMart/Capture Viewpoints")]
        public static void Run()
        {
            Directory.CreateDirectory(OutDir);
            var cam = Camera.main;
            var keepPos = cam.transform.position; var keepRot = cam.transform.rotation; float keepFov = cam.fieldOfView;
            // Probe realtime chưa có texture làm Forward+ của URP lỗi (ReflectionProbeManager NRE) → tạm tắt probe đó
            var off = new System.Collections.Generic.List<ReflectionProbe>();
            foreach (var p in Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None))
            {
                if (p.mode == UnityEngine.Rendering.ReflectionProbeMode.Realtime) p.RenderProbe();
                if (p.texture == null) { p.enabled = false; off.Add(p); Debug.LogWarning("Probe chưa có texture, tạm tắt khi chụp: " + p.name); }
            }
            foreach (var h in Object.FindObjectsByType<HemisphereAmbient>(FindObjectsSortMode.None)) h.Apply();
            foreach (var f in Object.FindObjectsByType<FilmSettings>(FindObjectsSortMode.None)) { f.grain = 0f; f.Push(); }
            cam.fieldOfView = 72f;
            foreach (JObject v in JArray.Parse(File.ReadAllText(ViewpointsPath)))
            {
                var pos = (JArray)v["pos"]; var tg = (JArray)v["target"];
                cam.transform.position = ThreeCoords.ToUnity((float)pos[0], (float)pos[1], (float)pos[2]);
                cam.transform.LookAt(ThreeCoords.ToUnity((float)tg[0], (float)tg[1], (float)tg[2]));
                File.WriteAllBytes($"{OutDir}/{(string)v["name"]}.png", Render(cam, 1280, 720));
            }
            foreach (var f in Object.FindObjectsByType<FilmSettings>(FindObjectsSortMode.None)) { f.grain = 0.045f; f.Push(); }
            foreach (var p in off) p.enabled = true;
            cam.transform.SetPositionAndRotation(keepPos, keepRot); cam.fieldOfView = keepFov;
        }

        static byte[] Render(Camera cam, int w, int h)
        {
            var rt = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var prev = cam.targetTexture; cam.targetTexture = rt; cam.Render(); cam.targetTexture = prev;
            var a = RenderTexture.active; RenderTexture.active = rt;
            var t = new Texture2D(w, h, TextureFormat.RGB24, false); t.ReadPixels(new Rect(0, 0, w, h), 0, 0); t.Apply();
            RenderTexture.active = a; RenderTexture.ReleaseTemporary(rt);
            var png = t.EncodeToPNG(); Object.DestroyImmediate(t); return png;
        }
    }
}
