// BW-MAP2.0B: seeded non-radial mainland, ocean inlets and marching-squares coastlines.
using System;
using System.Collections.Generic;
using Godot;

public static class MapConceptGenerator
{
    public const int GeneratorVersion = 2;
    private const float HalfWidth = MapConceptLayout.MapWidth * 0.5f;
    private const float HalfHeight = MapConceptLayout.MapHeight * 0.5f;
    private const float Spacing = 8f;
    private const int GridWidth = 201;
    private const int GridHeight = 136;
    private const int MaxAttempts = 5;

    #region Map generation

    // =========================================================
    // Reproducible bounded attempts preserve quality without a nondeterministic
    // "keep retrying until lucky" loop. Nothing here changes gameplay/world terrain.
    public static MapConceptLayout Generate(MapConceptParameters requested)
    {
        MapConceptParameters parameters = requested.Validated();
        MapConceptLayout? best = null;
        float bestScore = float.MinValue;

        for (int attempt = 0; attempt < MaxAttempts; attempt++)
        {
            ulong state = unchecked((ulong)parameters.Seed) +
                unchecked((ulong)attempt) * 0x9E3779B97F4A7C15UL;
            using var field = new LandField(parameters, state);
            float[] samples = SampleGrid(field);
            List<Vector2[]> contours = ExtractLandContours(samples, field);
            if (contours.Count == 0) continue;

            contours.Sort((a, b) => PolygonArea(b).CompareTo(PolygonArea(a)));
            Vector2[] coast = contours[0];
            float area = PolygonArea(coast);
            Rect2 bounds = Bounds(coast);
            float total = 0f;
            foreach (Vector2[] contour in contours) total += PolygonArea(contour);
            float fraction = area / Mathf.Max(total, 1f);
            float score = Mathf.Min(area / 160000f, 1f) * fraction;
            if (bounds.Size.X < 450f || bounds.Size.Y < 270f) score *= 0.45f;

            var candidate = MapConceptLayout.CreateProcedural(coast);
            // Natural fragments count toward the user's island limit.
            for (int i = 1; i < contours.Count && candidate.Islets.Count < parameters.OffshoreIslands; i++)
                if (PolygonArea(contours[i]) >= 650f &&
                    PolygonArea(contours[i]) < area * 0.23f)
                    candidate.Islets.Add(contours[i]);

            var extraRandom = new SeedStream(state ^ 0xD81956A02D35803FUL);
            AddOffshoreIslands(candidate, parameters.OffshoreIslands, ref extraRandom);

            if (score > bestScore)
            {
                best = candidate;
                bestScore = score;
            }
            if (area >= 105000f && bounds.Size.X >= 530f && bounds.Size.Y >= 320f &&
                fraction >= 0.76f) break;
        }

        // Explicit procedural-only safety fallback. Never accidentally attach
        // authored forests or towns to a procedural coastline.
        if (best != null) return best;
        var safeCoast = new Vector2[96];
        for (int i = 0; i < safeCoast.Length; i++)
        {
            float angle = Mathf.Tau * i / safeCoast.Length;
            safeCoast[i] = new Vector2(Mathf.Cos(angle) * 350f,
                Mathf.Sin(angle) * 255f);
        }
        return MapConceptLayout.CreateProcedural(safeCoast);
    }

    // =========================================================
    public static float LandPercent(MapConceptLayout map)
    {
        float area = PolygonArea(map.Coast);
        foreach (Vector2[] island in map.Islets) area += PolygonArea(island);
        return area / (MapConceptLayout.MapWidth * MapConceptLayout.MapHeight) * 100f;
    }

    private static float PolygonArea(Vector2[] polygon) => Mathf.Abs(SignedArea(polygon));

    private static float SignedArea(IReadOnlyList<Vector2> polygon)
    {
        double twiceArea = 0;
        for (int i = 0; i < polygon.Count; i++)
        {
            Vector2 a = polygon[i], b = polygon[(i + 1) % polygon.Count];
            twiceArea += (double)a.X * b.Y - (double)b.X * a.Y;
        }
        return (float)(twiceArea * 0.5);
    }

    private static Rect2 Bounds(Vector2[] points)
    {
        Vector2 low = points[0], high = points[0];
        foreach (Vector2 p in points) { low = low.Min(p); high = high.Max(p); }
        return new Rect2(low, high - low);
    }
    #endregion

    #region Continuous land and water field

    private readonly record struct Ellipse(Vector2 Position, float RadiusX, float RadiusY, float Angle);
    private readonly record struct PathPoint(Vector2 Position, float Radius);
    private sealed class PathShape
    {
        public readonly PathPoint[] Samples;
        public PathShape(PathPoint[] samples) => Samples = samples;
    }

    // =========================================================
    private sealed class LandField : IDisposable
    {
        private readonly FastNoiseLite _macro, _detail, _warpX, _warpY;
        private readonly List<Ellipse> _lands = new();
        private readonly List<PathShape> _arms = new(), _inlets = new();
        private readonly float _roughness, _complexity, _unionSmooth;
        private readonly Vector2 _centre;

        // =========================================================
        public LandField(MapConceptParameters p, ulong seed)
        {
            var rng = new SeedStream(seed);
            _roughness = p.CoastRoughness / 100f;
            _complexity = p.LandmassComplexity / 100f;
            float branching = p.BaysAndPeninsulas / 100f;
            float penetration = p.OceanPenetration / 100f;
            float scale = Mathf.Sqrt(p.LandCoverage / 55f);
            _centre = new Vector2(rng.Range(-52f, 52f), rng.Range(-30f, 30f));
            _macro = Noise(rng.NextInt(), 0.0036f, 3);
            _detail = Noise(rng.NextInt(), 0.019f, 2);
            _warpX = Noise(rng.NextInt(), 0.0035f, 2);
            _warpY = Noise(rng.NextInt(), 0.0035f, 2);
            _unionSmooth = 25f + 35f * _complexity;

            float aspect = rng.Range(0.74f, 1.28f);
            float rx = 285f * scale * Mathf.Sqrt(aspect);
            float ry = 215f * scale / Mathf.Sqrt(aspect);
            _lands.Add(new Ellipse(_centre, rx, ry, rng.Range(-0.55f, 0.55f)));

            // Separate overlapping cores break the elliptical silhouette.
            int bulges = 1 + rng.NextInt(1, 3 + (int)(_complexity * 3f));
            float firstDirection = rng.Range(0f, Mathf.Tau);
            for (int i = 0; i < bulges; i++)
            {
                float angle = firstDirection + Mathf.Tau * (i + rng.Range(-0.24f, 0.24f)) /
                    Mathf.Max(1f, bulges);
                Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
                float reach = rng.Range(125f, 285f) * scale;
                _lands.Add(new Ellipse(_centre + direction * reach,
                    rng.Range(130f, 250f) * scale, rng.Range(90f, 190f) * scale,
                    angle + rng.Range(-0.68f, 0.68f)));
            }

            // Curved tapered limbs allow coastlines with real branching and necks.
            int arms = 1 + rng.NextInt(1, 3 + (int)(_complexity * 4f + branching * 2f));
            float armPhase = rng.Range(0f, Mathf.Tau);
            for (int i = 0; i < arms; i++)
            {
                float angle = armPhase + Mathf.Tau * (i + rng.Range(-0.23f, 0.23f)) /
                    Mathf.Max(1f, arms);
                Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 side = new(-direction.Y, direction.X);
                float length = rng.Range(290f, 510f) * scale * (0.72f + 0.45f * branching);
                float bend = rng.Range(-170f, 170f) * (0.45f + 0.55f * _complexity);
                float width = rng.Range(52f, 120f) * (1f + branching * 0.25f);
                var points = new PathPoint[10];
                for (int j = 0; j < points.Length; j++)
                {
                    float t = j / (float)(points.Length - 1);
                    Vector2 position = _centre + direction * Mathf.Lerp(70f, length, t) +
                        side * bend * Mathf.Sin(t * Mathf.Pi);
                    float taper = Mathf.Lerp(1.24f, 0.43f, t);
                    points[j] = new PathPoint(position, width * taper);
                }
                _arms.Add(new PathShape(points));
            }

            // Ocean-connected paths: a wide outer bay narrows into a winding
            // estuary-like inland channel. Future freshwater rivers are separate.
            int inlets = penetration <= 0.04f ? 0 : 1 +
                rng.NextInt(0, 2 + (int)(penetration * 3f));
            float inletPhase = rng.Range(0f, Mathf.Tau);
            for (int i = 0; i < inlets; i++)
            {
                float angle = inletPhase + Mathf.Tau * (i + rng.Range(-0.26f, 0.26f)) /
                    Mathf.Max(1f, inlets);
                Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 side = new(-direction.Y, direction.X);
                float endDistance = Mathf.Lerp(315f, 85f, penetration) +
                    rng.Range(-45f, 65f);
                float bend = rng.Range(-200f, 200f) * (0.30f + 0.70f * penetration);
                float wiggle = rng.Range(-75f, 75f) * penetration;
                var points = new PathPoint[17];
                for (int j = 0; j < points.Length; j++)
                {
                    float t = j / (float)(points.Length - 1);
                    float reach = Mathf.Lerp(1000f, endDistance, t);
                    Vector2 position = _centre + direction * reach +
                        side * (bend * Mathf.Sin(Mathf.Pi * t) +
                            wiggle * Mathf.Sin(3f * Mathf.Pi * t) * t);
                    float mouth = Mathf.Lerp(75f, 11f, t);
                    float basin = 85f * penetration *
                        Mathf.Exp(-Mathf.Pow((t - 0.57f) / 0.17f, 2f));
                    points[j] = new PathPoint(position, (mouth + basin) *
                        Mathf.Lerp(0.55f, 1.20f, penetration));
                }
                _inlets.Add(new PathShape(points));
            }
        }

        // =========================================================
        public float Sample(Vector2 position)
        {
            float warp = 14f + _roughness * 44f + _complexity * 25f;
            Vector2 point = position + new Vector2(
                _warpX.GetNoise2D(position.X + 190f, position.Y) * warp,
                _warpY.GetNoise2D(position.X, position.Y - 370f) * warp);

            float land = float.NegativeInfinity;
            foreach (Ellipse ellipse in _lands)
                land = SmoothMax(land, EllipseDistance(point, ellipse), _unionSmooth);
            foreach (PathShape arm in _arms)
                land = SmoothMax(land, PathField(point, arm), _unionSmooth);

            land += _macro.GetNoise2D(point.X, point.Y) * (18f + 24f * _complexity);
            land += _detail.GetNoise2D(point.X, point.Y) * (4f + 22f * _roughness);

            foreach (PathShape inlet in _inlets)
                land = Mathf.Min(land, -PathField(point, inlet));

            // Sea border; gradual erosion near the frame, then guaranteed water
            // at the outer samples so marching-squares loops remain closed.
            float fx = Mathf.Max(0f, Mathf.Abs(position.X) - 632f);
            float fy = Mathf.Max(0f, Mathf.Abs(position.Y) - 386f);
            land -= fx * fx * 0.026f + fy * fy * 0.037f;
            land = Mathf.Min(land, Mathf.Min(
                757f - Mathf.Abs(position.X), 495f - Mathf.Abs(position.Y)));
            return land;
        }

        // =========================================================
        private static float EllipseDistance(Vector2 point, Ellipse e)
        {
            Vector2 delta = point - e.Position;
            float cos = Mathf.Cos(e.Angle), sin = Mathf.Sin(e.Angle);
            float x = (delta.X * cos + delta.Y * sin) / e.RadiusX;
            float y = (-delta.X * sin + delta.Y * cos) / e.RadiusY;
            return (1f - Mathf.Sqrt(x * x + y * y)) *
                Mathf.Min(e.RadiusX, e.RadiusY);
        }

        // =========================================================
        private static float PathField(Vector2 point, PathShape path)
        {
            float best = float.NegativeInfinity;
            for (int i = 0; i < path.Samples.Length - 1; i++)
            {
                PathPoint a = path.Samples[i], b = path.Samples[i + 1];
                Vector2 segment = b.Position - a.Position;
                float squared = segment.LengthSquared();
                float t = squared <= 0.00001f ? 0f :
                    Mathf.Clamp((point - a.Position).Dot(segment) / squared, 0f, 1f);
                float width = Mathf.Lerp(a.Radius, b.Radius, t);
                best = Mathf.Max(best, width - point.DistanceTo(a.Position + t * segment));
            }
            return best;
        }

        // =========================================================
        private static float SmoothMax(float a, float b, float k)
        {
            if (float.IsNegativeInfinity(a)) return b;
            float h = Mathf.Clamp(0.5f + 0.5f * (a - b) / k, 0f, 1f);
            return Mathf.Lerp(b, a, h) + k * h * (1f - h);
        }

        // =========================================================
        private static FastNoiseLite Noise(int seed, float frequency, int octaves) =>
            new()
            {
                Seed = seed,
                NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth,
                Frequency = frequency,
                FractalOctaves = octaves
            };

        // =========================================================
        public void Dispose()
        {
            _macro.Dispose();
            _detail.Dispose();
            _warpX.Dispose();
            _warpY.Dispose();
        }
    }
    #endregion

    #region Marching squares and connected coast contours

    // =========================================================
    private static float[] SampleGrid(LandField field)
    {
        var grid = new float[GridWidth * GridHeight];
        for (int y = 0; y < GridHeight; y++)
            for (int x = 0; x < GridWidth; x++)
                grid[y * GridWidth + x] = field.Sample(new Vector2(
                    -HalfWidth + x * Spacing, -HalfHeight + y * Spacing));
        return grid;
    }

    // =========================================================
    // Shared grid-edge IDs ensure contours join exactly across adjacent cells;
    // ambiguous diagonal cells are resolved using a centre-sample sign.
    private static List<Vector2[]> ExtractLandContours(float[] values, LandField field)
    {
        int horizontalCount = GridHeight * (GridWidth - 1);
        var ids = new Dictionary<int, int>();
        var vertices = new List<Vector2>();
        var adjacency = new Dictionary<int, List<int>>();
        for (int y = 0; y < GridHeight - 1; y++)
        for (int x = 0; x < GridWidth - 1; x++)
        {
            float a = values[y * GridWidth + x];
            float b = values[y * GridWidth + x + 1];
            float c = values[(y + 1) * GridWidth + x + 1];
            float d = values[(y + 1) * GridWidth + x];
            bool[] land = { a > 0f, b > 0f, c > 0f, d > 0f };
            int mask = (land[0] ? 1 : 0) | (land[1] ? 2 : 0) |
                (land[2] ? 4 : 0) | (land[3] ? 8 : 0);
            if (mask == 0 || mask == 15) continue;

            int[] edgeIds = { y * (GridWidth - 1) + x,
                horizontalCount + y * GridWidth + x + 1,
                (y + 1) * (GridWidth - 1) + x,
                horizontalCount + y * GridWidth + x };
            float[] corners = { a, b, c, d };
            Vector2[] positions = {
                new(-HalfWidth + x * Spacing, -HalfHeight + y * Spacing),
                new(-HalfWidth + (x + 1) * Spacing, -HalfHeight + y * Spacing),
                new(-HalfWidth + (x + 1) * Spacing, -HalfHeight + (y + 1) * Spacing),
                new(-HalfWidth + x * Spacing, -HalfHeight + (y + 1) * Spacing)
            };
            int[] edgeVertices = { -1, -1, -1, -1 };
            int count = 0;
            for (int edge = 0; edge < 4; edge++)
            {
                int next = (edge + 1) % 4;
                if (land[edge] == land[next]) continue;
                int key = edgeIds[edge];
                if (!ids.TryGetValue(key, out int id))
                {
                    float t = Mathf.Clamp(corners[edge] /
                        (corners[edge] - corners[next]), 0f, 1f);
                    id = vertices.Count;
                    vertices.Add(positions[edge].Lerp(positions[next], t));
                    ids.Add(key, id);
                }
                edgeVertices[edge] = id;
                count++;
            }

            if (count == 2)
            {
                int first = -1;
                for (int i = 0; i < 4; i++)
                    if (edgeVertices[i] >= 0)
                    {
                        if (first < 0) first = edgeVertices[i];
                        else Connect(first, edgeVertices[i], adjacency);
                    }
            }
            else if (count == 4)
            {
                bool centerLand = field.Sample(positions[0] +
                    new Vector2(Spacing * 0.5f, Spacing * 0.5f)) > 0f;
                for (int i = 0; i < 4; i++)
                    if (land[i] != centerLand)
                        Connect(edgeVertices[(i + 3) % 4], edgeVertices[i], adjacency);
            }
        }

        var loops = new List<Vector2[]>();
        var seen = new HashSet<int>();
        foreach (int start in adjacency.Keys)
        {
            if (seen.Contains(start)) continue;
            int cursor = start, previous = -1;
            var points = new List<Vector2>();
            bool closed = false;
            for (int i = 0; i < vertices.Count; i++)
            {
                if (!adjacency.TryGetValue(cursor, out var neighbours) ||
                    neighbours.Count != 2 || seen.Contains(cursor)) break;
                seen.Add(cursor);
                points.Add(vertices[cursor]);
                int next = neighbours[0] == previous ? neighbours[1] : neighbours[0];
                if (next == start) { closed = true; break; }
                previous = cursor;
                cursor = next;
            }
            if (!closed || points.Count < 9 || Mathf.Abs(SignedArea(points)) < 500f)
                continue;

            // Distinguish exterior land loops from interior water holes. The
            // latter are left for the future separate lake/rivers stage.
            float signedArea = SignedArea(points);
            Vector2 tangent = points[1] - points[0];
            Vector2 side = signedArea > 0f ?
                new Vector2(-tangent.Y, tangent.X) :
                new Vector2(tangent.Y, -tangent.X);
            Vector2 insidePoint = (points[0] + points[1]) * 0.5f +
                side.Normalized() * 2.5f;
            if (field.Sample(insidePoint) <= 0f) continue;

            Vector2[] simplified = SimplifyContour(points, 3.1f);
            if (simplified.Length >= 8) loops.Add(simplified);
        }
        return loops;
    }

    // =========================================================
    private static void Connect(int a, int b, Dictionary<int, List<int>> graph)
    {
        if (a < 0 || b < 0 || a == b) return;
        if (!graph.TryGetValue(a, out var left)) graph[a] = left = new List<int>(2);
        if (!graph.TryGetValue(b, out var right)) graph[b] = right = new List<int>(2);
        if (!left.Contains(b)) left.Add(b);
        if (!right.Contains(a)) right.Add(a);
    }

    // =========================================================
    // Two open RDP arcs preserve closed-loop corners while eliminating hundreds
    // of near-collinear marching-squares vertices before CanvasItem drawing.
    private static Vector2[] SimplifyContour(List<Vector2> ring, float tolerance)
    {
        int split = 1;
        float best = 0f;
        for (int i = 1; i < ring.Count; i++)
        {
            float distance = ring[0].DistanceSquaredTo(ring[i]);
            if (distance > best) { best = distance; split = i; }
        }
        var first = ring.GetRange(0, split + 1);
        var second = ring.GetRange(split, ring.Count - split);
        second.Add(ring[0]);
        List<Vector2> a = SimplifyOpen(first, tolerance);
        List<Vector2> b = SimplifyOpen(second, tolerance);
        a.RemoveAt(a.Count - 1);
        b.RemoveAt(b.Count - 1);
        a.AddRange(b);
        return a.ToArray();
    }

    // =========================================================
    private static List<Vector2> SimplifyOpen(List<Vector2> line, float tolerance)
    {
        var keep = new bool[line.Count];
        keep[0] = keep[^1] = true;
        var stack = new Stack<(int Start, int End)>();
        stack.Push((0, line.Count - 1));
        float epsilonSquared = tolerance * tolerance;
        while (stack.Count > 0)
        {
            var range = stack.Pop();
            Vector2 a = line[range.Start], b = line[range.End];
            Vector2 segment = b - a;
            float lengthSq = segment.LengthSquared();
            int farthest = -1;
            float maxDistanceSq = epsilonSquared;
            for (int i = range.Start + 1; i < range.End; i++)
            {
                float t = lengthSq <= 0.0001f ? 0f :
                    Mathf.Clamp((line[i] - a).Dot(segment) / lengthSq, 0f, 1f);
                float dist = line[i].DistanceSquaredTo(a + t * segment);
                if (dist > maxDistanceSq) { maxDistanceSq = dist; farthest = i; }
            }
            if (farthest < 0) continue;
            keep[farthest] = true;
            stack.Push((range.Start, farthest));
            stack.Push((farthest, range.End));
        }
        var result = new List<Vector2>();
        for (int i = 0; i < line.Count; i++)
            if (keep[i]) result.Add(line[i]);
        return result;
    }
    #endregion

    #region Offshore islands and seed utility

    // =========================================================
    private static void AddOffshoreIslands(MapConceptLayout map, int wanted, ref SeedStream rng)
    {
        // Natural split-off landmasses were already added by contour extraction.
        const int MaxRetries = 750;
        for (int attempt = 0; attempt < MaxRetries && map.Islets.Count < wanted; attempt++)
        {
            Vector2 center = new(rng.Range(-700f, 700f), rng.Range(-452f, 452f));
            float radius = rng.Range(16f, 32f);
            if (Mathf.Abs(center.X) + radius > 734f ||
                Mathf.Abs(center.Y) + radius > 476f || map.IsOnLand(center) ||
                DistanceToPolygon(center, map.Coast) > 220f)
                continue;

            var ring = new Vector2[14];
            float angleOffset = rng.Range(0f, Mathf.Tau);
            for (int i = 0; i < ring.Length; i++)
            {
                float a = angleOffset + Mathf.Tau * i / ring.Length;
                float radiusAtAngle = radius * rng.Range(0.80f, 1.16f);
                ring[i] = center + new Vector2(Mathf.Cos(a) * radiusAtAngle,
                    Mathf.Sin(a) * radiusAtAngle);
            }

            bool clear = true;
            foreach (Vector2 p in ring)
                if (map.IsOnLand(p) || DistanceToPolygon(p, map.Coast) < 16f)
                    { clear = false; break; }
            if (!clear) continue;
            foreach (Vector2[] island in map.Islets)
            {
                foreach (Vector2 p in ring)
                    if (MapConceptLayout.PointInPolygon(p, island) ||
                        DistanceToPolygon(p, island) < 18f)
                        { clear = false; break; }
                if (!clear) break;
            }
            if (clear) map.Islets.Add(ring);
        }
    }

    // =========================================================
    private static float DistanceToPolygon(Vector2 point, Vector2[] polygon)
    {
        float nearest = float.MaxValue;
        for (int i = 0; i < polygon.Length; i++)
        {
            Vector2 a = polygon[i], b = polygon[(i + 1) % polygon.Length];
            Vector2 edge = b - a;
            float t = edge.LengthSquared() < 0.0001f ? 0f :
                Mathf.Clamp((point - a).Dot(edge) / edge.LengthSquared(), 0f, 1f);
            nearest = Mathf.Min(nearest, point.DistanceTo(a + edge * t));
        }
        return nearest;
    }

    // =========================================================
    public static int DecorationSeed(long seed)
    {
        var rng = new SeedStream(unchecked((ulong)seed) ^ 0xBB47E5C13A2D109FUL);
        return rng.NextInt();
    }

    private struct SeedStream
    {
        private ulong _state;
        public SeedStream(ulong seed) => _state = seed;

        // =========================================================
        private ulong Next64()
        {
            unchecked
            {
                ulong x = (_state += 0x9E3779B97F4A7C15UL);
                x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
                x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
                return x ^ (x >> 31);
            }
        }

        public int NextInt() => unchecked((int)Next64());
        public int NextInt(int min, int max) => min + (int)(Next64() % (uint)(max - min));
        public float Range(float min, float max) =>
            min + (float)((Next64() >> 40) / 16777216.0) * (max - min);
    }
    #endregion
}
