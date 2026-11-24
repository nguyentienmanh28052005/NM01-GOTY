using System;
using UnityEngine;

namespace HanaMart.Store
{
    /// Bảng thay hàng hóa: loại hàng bản Three.js → các prefab từ asset siêu thị + tên hiển thị trong game.
    [CreateAssetMenu(menuName = "HanaMart/Product Catalog")]
    public class ProductCatalog : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string threeType;
            public string displayName;
            public GameObject[] prefabs;
        }
        public Entry[] entries = Array.Empty<Entry>();

        public Entry Find(string threeType) => Array.Find(entries, e => e.threeType == threeType);
    }
}
