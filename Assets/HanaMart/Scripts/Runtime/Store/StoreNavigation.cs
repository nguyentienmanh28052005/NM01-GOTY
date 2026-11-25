using System;
using System.Collections.Generic;
using System.Linq;
using HanaMart.Player;
using UnityEngine;

namespace HanaMart.Store
{
    public sealed class StoreNavigation
    {
        public const float CellSize = .15f;
        const float Radius = .24f;
        const float X0 = -6, Z0 = -9.6f;
        const int Width = 160, Depth = 171;
        const int NearestLimit = 24, SearchLimit = 60000;

        // Rectangles in Three coordinates. The last three are the only passages between zones.
        static readonly Rect[] Areas =
        {
            Rect.MinMaxRect(.24f, .24f, 11.76f, 7.76f),
            Rect.MinMaxRect(.24f, -4.36f, 4.36f, -.32f),
            Rect.MinMaxRect(-1.16f, -8.86f, 8.16f, -4.84f),
            Rect.MinMaxRect(-6, 8.39f, 18, 16),
            Rect.MinMaxRect(2.45f, 7.6f, 3.75f, 8.6f),
            Rect.MinMaxRect(.45f, -.5f, 1.15f, .5f),
            Rect.MinMaxRect(1.75f, -5, 2.45f, -4.2f),
        };
        static readonly Vector2Int[] Directions =
        {
            Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down,
            new(1, 1), new(1, -1), new(-1, 1), new(-1, -1),
        };
        readonly bool[] grid = new bool[Width * Depth];

        public StoreNavigation(IEnumerable<Box2> colliders)
        {
            var obstacles = colliders.Where(box => box != null && !box.off && !IsPassageDoor(box)).ToArray();
            for (int z = 0; z < Depth; ++z)
                for (int x = 0; x < Width; ++x)
                {
                    var center = Center(new Vector2Int(x, z));
                    grid[z * Width + x] = InsideAreas(center) && !InsideObstacle(center, obstacles);
                }
        }

        static bool InsideAreas(Vector2 point)
        {
            foreach (var area in Areas)
                if (point.x >= area.xMin && point.x <= area.xMax && point.y >= area.yMin && point.y <= area.yMax)
                    return true;
            return false;
        }

        static bool InsideObstacle(Vector2 point, Box2[] obstacles)
        {
            foreach (var box in obstacles)
                if (point.x > box.x0 - Radius && point.x < box.x1 + Radius && point.y > box.z0 - Radius && point.y < box.z1 + Radius)
                    return true;
            return false;
        }

        static bool IsPassageDoor(Box2 box) =>
            (Mathf.Abs(box.x0 - 2.3f) < .001f && Mathf.Abs(box.z0 - 7.95f) < .001f) ||
            (Mathf.Abs(box.x0 - 1.6f) < .001f && Mathf.Abs(box.z0 + 4.68f) < .001f);

        static Vector2Int Cell(Vector2 point) => new(Mathf.FloorToInt((point.x - X0) / CellSize),
            Mathf.FloorToInt((point.y - Z0) / CellSize));
        static Vector2 Center(Vector2Int point) => new(X0 + (point.x + .5f) * CellSize,
            Z0 + (point.y + .5f) * CellSize);
        static int Index(Vector2Int cell) => cell.y * Width + cell.x;
        bool Free(Vector2Int cell) => cell.x >= 0 && cell.x < Width && cell.y >= 0 && cell.y < Depth && grid[Index(cell)];
        public bool Walkable(float x, float z) => Free(Cell(new Vector2(x, z)));

        public bool LineFree(Vector2 start, Vector2 end)
        {
            var previous = Cell(start);
            if (!Free(previous) || !Free(Cell(end))) return false;
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(start, end) / (CellSize * .5f)));
            for (int sample = 1; sample <= steps; ++sample)
            {
                var next = Cell(Vector2.Lerp(start, end, (float)sample / steps));
                if (!Free(next) || !CanCrossCorner(previous, next)) return false;
                previous = next;
            }
            return true;
        }

        bool CanCrossCorner(Vector2Int start, Vector2Int end) => start.x == end.x || start.y == end.y ||
            (Free(new Vector2Int(start.x, end.y)) && Free(new Vector2Int(end.x, start.y)));

        bool TryNearest(Vector2Int cell, out Vector2Int nearest)
        {
            nearest = cell;
            if (Free(cell)) return true;
            if (cell.x < -NearestLimit || cell.y < -NearestLimit || cell.x >= Width + NearestLimit || cell.y >= Depth + NearestLimit)
                return false;
            for (int radius = 1; radius <= NearestLimit; ++radius)
                for (int z = -radius; z <= radius; ++z)
                    for (int x = -radius; x <= radius; ++x)
                    {
                        if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)) != radius) continue;
                        var point = cell + new Vector2Int(x, z);
                        if (!Free(point)) continue;
                        nearest = point;
                        return true;
                    }
            return false;
        }

        public bool TryPath(Vector2 start, Vector2 end, out IReadOnlyList<Vector2> waypoints)
        {
            waypoints = Array.Empty<Vector2>();
            if (LineFree(start, end))
            {
                waypoints = new[] { end };
                return true;
            }
            if (!TryNearest(Cell(start), out var origin) || !TryNearest(Cell(end), out var goal)) return false;

            var from = new int[grid.Length];
            var cost = new float[grid.Length];
            var closed = new bool[grid.Length];
            Array.Fill(from, -1);
            Array.Fill(cost, float.PositiveInfinity);
            cost[Index(origin)] = 0;
            var open = new List<(Vector2Int cell, float rank)> { (origin, 0) };
            for (int step = 0; step < SearchLimit && open.Count > 0; ++step)
            {
                int lowest = 0;
                for (int i = 1; i < open.Count; ++i)
                    if (open[i].rank < open[lowest].rank) lowest = i;
                var current = open[lowest].cell;
                open[lowest] = open[open.Count - 1];
                open.RemoveAt(open.Count - 1);
                int key = Index(current);
                if (closed[key]) continue;
                if (current == goal)
                {
                    waypoints = Simplify(from, key, start, end);
                    return true;
                }
                closed[key] = true;
                foreach (var direction in Directions)
                {
                    var next = current + direction;
                    if (!Free(next) || !CanCrossCorner(current, next)) continue;
                    int index = Index(next);
                    float distance = cost[key] + (direction.x != 0 && direction.y != 0 ? 1.414f : 1);
                    if (closed[index] || distance >= cost[index]) continue;
                    cost[index] = distance;
                    from[index] = key;
                    open.Add((next, distance + (next - goal).magnitude));
                }
            }
            return false;
        }

        List<Vector2> Simplify(int[] from, int key, Vector2 start, Vector2 end)
        {
            var points = new List<Vector2>();
            for (int count = 0; count < from.Length && key >= 0; ++count)
            {
                points.Add(Center(new Vector2Int(key % Width, key / Width)));
                key = from[key];
            }
            points.Reverse();
            var path = new List<Vector2>();
            for (int next = 0; next < points.Count; ++next)
            {
                for (int far = points.Count - 1; far > next; --far)
                {
                    if (!LineFree(start, points[far])) continue;
                    next = far;
                    break;
                }
                start = points[next];
                path.Add(start);
            }
            if (LineFree(start, end) && start != end) path.Add(end);
            return path;
        }

        public Mesh CreateSurfaceMesh()
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (int z = 0; z < Depth; ++z)
                for (int x = 0; x < Width; ++x)
                {
                    if (!Free(new Vector2Int(x, z))) continue;
                    int first = x;
                    while (x + 1 < Width && Free(new Vector2Int(x + 1, z))) ++x;
                    float left = X0 + first * CellSize, right = X0 + (x + 1) * CellSize;
                    float near = Z0 + z * CellSize, far = Z0 + (z + 1) * CellSize;
                    int corner = vertices.Count;
                    vertices.Add(ThreeCoords.ToUnity(left, 0, near));
                    vertices.Add(ThreeCoords.ToUnity(right, 0, near));
                    vertices.Add(ThreeCoords.ToUnity(right, 0, far));
                    vertices.Add(ThreeCoords.ToUnity(left, 0, far));
                    triangles.AddRange(new[] { corner, corner + 1, corner + 2, corner, corner + 2, corner + 3 });
                }
            var mesh = new Mesh { name = "Store walkable cells" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            return mesh;
        }
    }
}
