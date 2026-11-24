using UnityEngine;

namespace HanaMart.Store
{
    public sealed class HingeMotor
    {
        public float open, target, rate, radians;
        public Vector3 axis = Vector3.up;
        public HingeMotor(float radians, float rate) { this.radians = radians; this.rate = rate; }
        public void Tick(float dt) => open += (target - open) * Mathf.Clamp01(dt * rate);
        public Quaternion Rotation => Quaternion.AngleAxis(open * radians * Mathf.Rad2Deg, axis);
    }
}
