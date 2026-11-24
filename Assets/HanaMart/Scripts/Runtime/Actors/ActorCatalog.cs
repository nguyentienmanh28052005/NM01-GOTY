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
        }
        public Entry[] models=Array.Empty<Entry>();
        public AnimationClip walk;
    }
}
