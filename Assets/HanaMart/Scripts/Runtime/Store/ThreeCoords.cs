using UnityEngine;

namespace HanaMart.Store
{
    /// Đổi tọa độ Three.js (tay phải) sang Unity như glTFast: đảo dấu X.
    public static class ThreeCoords
    {
        public static Vector3 ToUnity(float x, float y, float z) => new Vector3(-x, y, z);
        public static Vector3 ToUnity(Vector3 p) => new Vector3(-p.x, p.y, p.z);
        // Three camera uses YXZ and looks down local -Z. Unity Euler applies Z, X, Y.
        public static Quaternion CameraRotation(float pitch, float yaw, float roll) =>
            Quaternion.Euler(-pitch * Mathf.Rad2Deg, 180f - yaw * Mathf.Rad2Deg, roll * Mathf.Rad2Deg);
    }
}
