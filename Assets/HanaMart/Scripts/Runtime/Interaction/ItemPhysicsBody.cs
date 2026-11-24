using HanaMart.Store;
using UnityEngine;

namespace HanaMart.Interaction
{
    /// Deterministic physics.js integrator for shelf spills; independent of player blocking boxes.
    public sealed class ItemPhysicsBody : MonoBehaviour
    {
        public ItemBody Body { get; set; }
        public event System.Action<float> Landed;
        public event System.Action Slept;
        void FixedUpdate()
        {
            if (Body==null || Body.sleeping)return;
            int lands=Body.lands;float impact=-Body.velocity.y;
            ItemPhysics.Step(Body,Time.fixedDeltaTime);
            transform.position=ThreeCoords.ToUnity(Body.position);
            var q=Body.rotation;transform.rotation=new Quaternion(q.x,-q.y,-q.z,q.w);
            if(Body.lands>lands)Landed?.Invoke(impact);
            if(Body.sleeping)Slept?.Invoke();
        }
    }
}
