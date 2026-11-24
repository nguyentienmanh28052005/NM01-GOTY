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
        // Ánh sáng môi trường của "phòng trắng có dải đèn" (store._env của bản Three.js) — gần đúng bằng 3 màu
        public Color envSky = new Color(0.9f, 0.9f, 0.95f), envEquator = new Color(0.5f, 0.5f, 0.55f), envGround = new Color(0.25f, 0.25f, 0.3f);
        public float envScale = 1.15f; // chỉnh theo ảnh so sánh với bản Three.js
        void OnEnable() => Apply();
        void OnValidate() => Apply();
        public void Apply()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            float k = intensity * scale;
            float e = envScale * scale;
            RenderSettings.ambientSkyColor = sky * k + envSky * e;
            RenderSettings.ambientEquatorColor = Color.Lerp(sky, ground, 0.5f) * k + envEquator * e;
            RenderSettings.ambientGroundColor = ground * k + envGround * e;
        }
    }
}
