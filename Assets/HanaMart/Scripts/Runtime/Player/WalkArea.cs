using System.Collections.Generic;

namespace HanaMart.Player
{
    public class WalkArea
    {
        public const float R = 0.28f;
        public static readonly float[][] Zones = { new[] { 0f, 0f, 12f, 8f }, new[] { 0f, -4.6f, 4.6f, 0f }, new[] { -1.4f, -9.1f, 8.4f, -4.6f } };
        public readonly List<Box2> colliders;
        public WalkArea(List<Box2> colliders) { this.colliders = colliders ?? new List<Box2>(); }
        /// Như walkable() của player.js: phải nằm trong một vùng, và cách mọi hộp va chạm (đang bật) ít nhất R.
        public bool Walkable(float x, float z)
        {
            bool inZone = false;
            foreach (var zn in Zones) if (x >= zn[0] && x <= zn[2] && z >= zn[1] && z <= zn[3]) { inZone = true; break; }
            if (!inZone) return false;
            foreach (var c in colliders)
            {
                if (c == null || c.off) continue;
                if (x > c.x0 - R && x < c.x1 + R && z > c.z0 - R && z < c.z1 + R) return false;
            }
            return true;
        }
    }
}
