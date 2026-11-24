using UnityEngine;

namespace HanaMart.Render
{
    /// Tham số shader Film (giống uniform của bản Three.js). Game logic chỉnh các trường này; mỗi khung đẩy lên shader toàn cục.
    [ExecuteAlways]
    public class FilmSettings : MonoBehaviour
    {
        public float exposure = 1.02f;
        [Range(0, 1)] public float cine = 0f, pulse = 0f, warp = 0f, glitch = 0f, red = 0f, fade = 0f, white = 0f;
        public float grain = 0.045f, vignette = 0.5f, ca = 0.0018f, bright = 1f, sat = 1.22f;

        static readonly int Exposure = Shader.PropertyToID("_HM_Exposure"), Time_ = Shader.PropertyToID("_HM_Time"), Cine = Shader.PropertyToID("_HM_Cine"),
            Pulse = Shader.PropertyToID("_HM_Pulse"), Warp = Shader.PropertyToID("_HM_Warp"), Grain = Shader.PropertyToID("_HM_Grain"), Vignette = Shader.PropertyToID("_HM_Vignette"),
            CA = Shader.PropertyToID("_HM_CA"), Fade = Shader.PropertyToID("_HM_Fade"), White = Shader.PropertyToID("_HM_White"), Glitch = Shader.PropertyToID("_HM_Glitch"),
            Bright = Shader.PropertyToID("_HM_Bright"), Red = Shader.PropertyToID("_HM_Red"), Sat = Shader.PropertyToID("_HM_Sat");

        void OnEnable() => Push();
        void Update() => Push();

        public void Push()
        {
            Shader.SetGlobalFloat(Exposure, exposure); Shader.SetGlobalFloat(Time_, Application.isPlaying ? Time.time : 0f);
            Shader.SetGlobalFloat(Cine, cine); Shader.SetGlobalFloat(Pulse, pulse); Shader.SetGlobalFloat(Warp, warp); Shader.SetGlobalFloat(Grain, grain);
            Shader.SetGlobalFloat(Vignette, vignette); Shader.SetGlobalFloat(CA, ca); Shader.SetGlobalFloat(Fade, fade); Shader.SetGlobalFloat(White, white);
            Shader.SetGlobalFloat(Glitch, glitch); Shader.SetGlobalFloat(Bright, bright); Shader.SetGlobalFloat(Red, red); Shader.SetGlobalFloat(Sat, sat);
        }
    }
}
