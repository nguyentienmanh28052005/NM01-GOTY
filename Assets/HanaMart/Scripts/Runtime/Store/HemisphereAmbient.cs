using UnityEngine;
using UnityEngine.Rendering;

namespace HanaMart.Store
{
    /// HemisphereLight của Three.js → ánh sáng môi trường 3 màu của Unity (áp dụng khi scene chạy).
    [ExecuteAlways]
    public class HemisphereAmbient : MonoBehaviour
    {
        public Color sky = Color.white, ground = Color.black;
        public float intensity = 1f;
        [Range(0, 2)] public float scale = 1f; // để làm tối khi mất điện
        void OnEnable() => Apply();
        void OnValidate() => Apply();
        public void Apply()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            float k = intensity * scale;
            RenderSettings.ambientSkyColor = sky * k;
            RenderSettings.ambientEquatorColor = Color.Lerp(sky, ground, 0.5f) * k;
            RenderSettings.ambientGroundColor = ground * k;
        }
    }
}
