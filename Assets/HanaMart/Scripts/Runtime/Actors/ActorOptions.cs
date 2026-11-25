using System;
using UnityEngine;

namespace HanaMart.Actors
{
    [Serializable]
    public sealed class ActorOptions
    {
        public string coat="padding",face="normal",mask;
        public int color=0x1d1f24,pants=0x23252b,hair=0x121214,skin=0xe2b998,modelIndex=-1;
        public float scale=1,tall=1,uncanny,tilt;
        public bool pale,hood,cap,longHair,noShadow,rainShell;
        public Vector3 Scale
        {
            get {float s=coat=="child"?.68f:scale==0?1:scale;return new Vector3(s,s*(tall==0?1:tall),s);}
        }
        public int ModelIndex => modelIndex>=0 ? Mathf.Clamp(modelIndex,0,3) : longHair ? 0 : coat=="raincoat" || coat=="manager" ? 1 : coat=="child" ? 2 : 3;
    }
}
