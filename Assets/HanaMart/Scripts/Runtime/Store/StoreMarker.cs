using UnityEngine;

namespace HanaMart.Store
{
    /// Đánh dấu một object nhập từ cảnh Three.js: khóa gốc (S:/R:/P:/L:/H:/C:) và extras (JSON).
    public class StoreMarker : MonoBehaviour
    {
        public enum Kind { Static, Ref, Product, Light, Hit, Collider, ShaderMesh, Skipped }
        public Kind kind;
        public string key;
        [TextArea] public string json;
    }
}
