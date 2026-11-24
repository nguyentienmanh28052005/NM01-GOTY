using UnityEngine;

namespace HanaMart.Store
{
    /// Đổi tọa độ Three.js (tay phải) sang Unity như glTFast: đảo dấu X.
    public static class ThreeCoords
    {
        public static Vector3 ToUnity(float x, float y, float z) => new Vector3(-x, y, z);
        public static Vector3 ToUnity(Vector3 p) => new Vector3(-p.x, p.y, p.z);
    }
}
