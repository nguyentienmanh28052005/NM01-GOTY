using System;
using UnityEngine;

namespace HanaMart.Audio
{
    [CreateAssetMenu(menuName="HanaMart/Audio Bank")]
    public sealed class AudioBank : ScriptableObject
    {
        [Serializable] public class Entry { public string id; public AudioClip clip; }
        public Entry[] entries=Array.Empty<Entry>();
        public AudioClip Find(string id) => Array.Find(entries,e=>e.id==id)?.clip;
    }
}
