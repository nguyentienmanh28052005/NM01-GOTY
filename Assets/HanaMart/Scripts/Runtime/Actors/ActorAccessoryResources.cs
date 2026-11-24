using UnityEngine;

namespace HanaMart.Actors
{
    public sealed class ActorAccessoryResources : MonoBehaviour
    {
        public Mesh mesh;
        public Material material;
        public Texture2D region,image;
        void OnDestroy(){Release(mesh);Release(material);Release(region);Release(image);}
        static void Release(Object value){if(!value)return;if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
    }
}
