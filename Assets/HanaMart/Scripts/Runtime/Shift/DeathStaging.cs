using System;
using UnityEngine;

namespace HanaMart.Shift
{
    public static class DeathStaging
    {
        public static Vector3 FindSpot(Vector3 eye, Vector3 forward, float distance, float preference, Func<float, float, bool> walkable)
        {
            var flat = new Vector3(forward.x, 0, forward.z);
            if (flat.sqrMagnitude < .0001f) flat = Vector3.forward;
            else flat.Normalize();

            foreach (float angle in new[]
            {
                preference, preference + .6f, preference - .6f, preference + 1.2f,
                preference - 1.2f, preference + 1.8f, preference - 1.8f, Mathf.PI,
            })
            {
                if (TryDirection(RotateHorizontal(flat, angle), distance, out var hit)) return hit;
            }

            var prefDir = RotateHorizontal(flat, preference);
            var cardinals = new[] { Vector3.forward, Vector3.back, Vector3.right, Vector3.left };
            Array.Sort(cardinals, (a, b) => Vector3.Dot(b, prefDir).CompareTo(Vector3.Dot(a, prefDir)));
            foreach (float tryDist in new[] { distance, Mathf.Max(.45f, distance * .65f) })
                foreach (var dir in cardinals)
                    if (TryDirection(dir, tryDist, out var hit)) return hit;

            float fallbackDist = Mathf.Min(distance, .55f);
            return new Vector3(eye.x + prefDir.x * fallbackDist, 0, eye.z + prefDir.z * fallbackDist);

            bool TryDirection(Vector3 direction, float span, out Vector3 point)
            {
                for (float step = .25f; step <= span; step += .12f)
                {
                    var sample = eye + direction * step;
                    if (!walkable(sample.x, sample.z)) { point = default; return false; }
                }
                var end = eye + direction * span;
                if (!walkable(end.x, end.z)) { point = default; return false; }
                point = new Vector3(end.x, 0, end.z);
                return true;
            }
        }

        static Vector3 RotateHorizontal(Vector3 direction, float radians)
        {
            float c = Mathf.Cos(radians), s = Mathf.Sin(radians);
            return new Vector3(direction.x * c - direction.z * s, 0, direction.x * s + direction.z * c);
        }

        public static Vector3 BoxOrigin(int rule, Vector3 eye, Vector3 forward)
        {
            if (rule == 8 && Vector2.Distance(new Vector2(eye.x, eye.z), new Vector2(3, -5.6f)) < 3.5f)
                return new Vector3(3, 0, -5.55f);
            return new Vector3(eye.x + forward.x * 1.2f, 0, eye.z + forward.z * 1.2f);
        }

        public static float AlleySpace(Vector3 eye, Vector3 direction)
        {
            for (int tenth = 5; tenth < 65; ++tenth)
            {
                float length = tenth / 10f;
                var point = eye + direction * length;
                if (point.z < -9 || point.z > -4.7f || point.x < -1.3f || point.x > 8.3f) return length - .2f;
            }
            return 6.5f;
        }

        public static bool FiredProductInView(Vector3 product, Vector3 eye, Vector3 forward)
        {
            if (product.z <= .9f || product.z >= 7 || product.x <= 3) return false;
            var delta = new Vector2(product.x - eye.x, product.z - eye.z);
            float distance = delta.magnitude;
            return distance < 7 && distance > .8f && Vector2.Dot(delta, new Vector2(forward.x, forward.z)) > distance * .45f;
        }
    }
}
