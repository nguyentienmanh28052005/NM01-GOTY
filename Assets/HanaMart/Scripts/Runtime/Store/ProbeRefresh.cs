using UnityEngine;

namespace HanaMart.Store
{
    /// Chụp Reflection Probe một lần khi vào cảnh (như captureProbe của bản Three.js, chụp lúc đủ đèn).
    [RequireComponent(typeof(ReflectionProbe))]
    public class ProbeRefresh : MonoBehaviour
    {
        void Start() => GetComponent<ReflectionProbe>().RenderProbe();
    }
}
