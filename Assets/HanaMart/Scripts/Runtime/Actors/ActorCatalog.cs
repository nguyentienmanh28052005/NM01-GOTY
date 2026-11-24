using System;
using UnityEngine;

namespace HanaMart.Actors
{
    [CreateAssetMenu(menuName="HanaMart/Actor Catalog")]
    public sealed class ActorCatalog : ScriptableObject
    {
        [Serializable] public sealed class Entry
        {
            public string id;
            public GameObject model;
            public Avatar avatar;
            public RendererMaterials[] renderers=Array.Empty<RendererMaterials>();
        }
        [Serializable] public sealed class RendererMaterials
        {
            public string path;
            public Material[] materials=Array.Empty<Material>();
            public Material[] rainMaterials=Array.Empty<Material>();
        }
        public Entry[] models=Array.Empty<Entry>();
        public AnimationClip walk;
    }
}
