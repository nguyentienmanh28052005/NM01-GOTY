using System;
using System.Collections.Generic;
using UnityEngine;

namespace HanaMart.Store
{
    /// <summary>
    /// Helpers for upright package orientation and convenience-store bay color blocking.
    /// </summary>
    public static class ProductPlanogram
    {
        public struct BlockSlot
        {
            public int zone;
            public int row;
            public int col;
        }

        /// <summary>
        /// Computes an orthonormal rotation that aligns <paramref name="frontNormal"/> to +Z
        /// and label-up (-<paramref name="dPdv"/>) to +Y without flipping upside down.
        /// </summary>
        public static Quaternion ComputeUprightAlignment(Vector3 frontNormal, Vector3 dPdv)
        {
            Vector3 front = frontNormal.sqrMagnitude > 1e-6f ? frontNormal.normalized : Vector3.forward;
            Vector3 up = -dPdv;
            up -= Vector3.Dot(up, front) * front;

            if (up.sqrMagnitude < 1e-6f)
            {
                up = Mathf.Abs(Vector3.Dot(front, Vector3.up)) < 0.99f ? Vector3.up : Vector3.right;
                up -= Vector3.Dot(up, front) * front;
            }
            up.Normalize();

            return Quaternion.Inverse(Quaternion.LookRotation(front, up));
        }

        /// <summary>
        /// Perceptual RGB color distance (redmean-weighted).
        /// </summary>
        public static float ColorDistance(Color a, Color b)
        {
            float rmean = 0.5f * (a.r + b.r);
            float dr = a.r - b.r;
            float dg = a.g - b.g;
            float db = a.b - b.b;
            return Mathf.Sqrt((2f + rmean) * dr * dr + 4f * dg * dg + (3f - rmean) * db * db);
        }

        /// <summary>
        /// Assigns SKU indices to shelf slots using horizontal/vertical color-blocking contrast
        /// while maximizing palette variety across the bay.
        /// </summary>
        public static int[] AssignColorBlocks(BlockSlot[] slots, Color[] palette, int seed = 0)
        {
            if (slots == null || slots.Length == 0)
                return Array.Empty<int>();

            int[] result = new int[slots.Length];
            if (palette == null || palette.Length <= 1)
                return result;

            int n = palette.Length;
            int[] order = new int[slots.Length];
            for (int i = 0; i < slots.Length; i++) order[i] = i;

            Array.Sort(order, (a, b) =>
            {
                int cmp = slots[a].zone.CompareTo(slots[b].zone);
                if (cmp != 0) return cmp;
                cmp = slots[a].row.CompareTo(slots[b].row);
                if (cmp != 0) return cmp;
                return slots[a].col.CompareTo(slots[b].col);
            });

            var assignedByCoord = new Dictionary<(int zone, int row, int col), int>(slots.Length);
            int[] usedCount = new int[n];
            int currentZone = int.MinValue;

            foreach (int slotIdx in order)
            {
                var s = slots[slotIdx];
                if (s.zone != currentZone)
                {
                    currentZone = s.zone;
                    Array.Clear(usedCount, 0, n);
                }

                bool hasLeft = assignedByCoord.TryGetValue((s.zone, s.row, s.col - 1), out int leftSku);
                bool hasBelow = assignedByCoord.TryGetValue((s.zone, s.row - 1, s.col), out int belowSku);

                int bestSku = 0;
                float bestScore = float.NegativeInfinity;

                for (int p = 0; p < n; p++)
                {
                    float score = 0f;
                    if (hasLeft && p == leftSku) score -= 1000f;
                    if (hasBelow && p == belowSku) score -= 1000f;

                    float minDist = 3f;
                    if (hasLeft) minDist = Mathf.Min(minDist, ColorDistance(palette[p], palette[leftSku]));
                    if (hasBelow) minDist = Mathf.Min(minDist, ColorDistance(palette[p], palette[belowSku]));

                    if (hasLeft || hasBelow)
                        score += Mathf.Min(minDist, 1.2f);

                    score -= usedCount[p] * 0.35f;
                    score += ((p + seed + s.row * 3 + s.col * 7) & 0x7FFFFFFF) % n * 1e-4f;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestSku = p;
                    }
                }

                result[slotIdx] = bestSku;
                assignedByCoord[(s.zone, s.row, s.col)] = bestSku;
                usedCount[bestSku]++;
            }

            return result;
        }
    }
}
