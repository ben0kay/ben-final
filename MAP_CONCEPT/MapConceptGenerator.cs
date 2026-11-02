// BW-MAP2.0: deterministic FastNoiseLite coastline and bounded offshore-island generator.
using System;
using System.Collections.Generic;
using Godot;

public static class MapConceptGenerator
{
    public const int GeneratorVersion = 1;
    private const int CoastSamples = 160;
    private const int IsletSamples = 13;
    private const float SafeHalfWidth = 746f;
    private const float SafeHalfHeight = 488f;

    #region Main island

    // =========================================================
    // Polar contours guarantee one connected, non-self-crossing, island-shaped
    // primary polygon. Low-frequency noise controls macro bays; high-frequency
    // noise only changes coastline detail. All values depend on the full 64-bit seed.
    public static MapConceptLayout Generate(MapConceptParameters requested)
    {
        MapConceptParameters parameters = requested.Validated();
        var random = new SeedStream(unchecked((ulong)parameters.Seed));

        // Noise Resources belong only to this regeneration pass; release immediately.
        using var macro = MakeNoise(random.NextInt(), 0.39f, 3);
        using var detail = MakeNoise(random.NextInt(), 1.34f, 2);
        using var warp = MakeNoise(random.NextInt(), 0.61f, 2);

        float coverage = Mathf.Sqrt(parameters.LandCoverage / 60f);
        float roughness = parameters.CoastRoughness / 100f;
        float peninsulas = parameters.BaysAndPeninsulas / 100f;

        // Seed-dependent axis ratio and rotation make different worlds visually distinct.
        float aspect = random.Range(0.75f, 1.26f);
        float axisX = 688f * coverage * Mathf.Sqrt(aspect);
        float axisY = 464f * coverage / Mathf.Sqrt(aspect);
        float rotation = random.Range(-0.43f, 0.43f);
        float cosine = Mathf.Cos(rotation), sine = Mathf.Sin(rotation);
        int lobeCount = random.NextInt(2, 6);
        float lobePhase = random.Range(0f, Mathf.Tau);
        float offsetX = random.Range(-45f, 45f), offsetY = random.Range(-27f, 27f);
        float sampleOffsetX = random.Range(-90f, 90f), sampleOffsetY = random.Range(-90f, 90f);
        var outline = new Vector2[CoastSamples];

        for (int i = 0; i < CoastSamples; i++)
        {
            float angle = Mathf.Tau * i / CoastSamples;
            float x = Mathf.Cos(angle), y = Mathf.Sin(angle);
            float phaseWarp = warp.GetNoise2D(x * 3.5f + sampleOffsetX,
                y * 3.5f + sampleOffsetY) * 0.65f * peninsulas;
            float wx = Mathf.Cos(angle + phaseWarp), wy = Mathf.Sin(angle + phaseWarp);
            float broad = macro.GetNoise2D(wx * 3.2f + sampleOffsetX,
                wy * 3.2f + sampleOffsetY);
            float fine = detail.GetNoise2D(x * 6.5f + sampleOffsetX,
                y * 6.5f + sampleOffsetY);
            float bays = Mathf.Sin(lobeCount * angle + lobePhase);
            float radius = Mathf.Clamp(1f + broad * (0.18f + roughness * 0.25f) +
                fine * (0.025f + roughness * 0.12f) +
                bays * (0.055f + peninsulas * 0.19f), 0.55f, 1.50f);

            float u = x * axisX * radius, v = y * axisY * radius;
            outline[i] = new Vector2(u * cosine - v * sine + offsetX,
                u * sine + v * cosine + offsetY);
        }

        // Fit to an inset canvas frame while preserving shape/aspect; no clipped land.
        float extentX = 1f, extentY = 1f;
        foreach (Vector2 p in outline)
        {
            extentX = Mathf.Max(extentX, Mathf.Abs(p.X));
            extentY = Mathf.Max(extentY, Mathf.Abs(p.Y));
        }
        float shrink = Mathf.Min(1f,
            Mathf.Min(SafeHalfWidth / extentX, SafeHalfHeight / extentY));
        for (int i = 0; i < outline.Length; i++) outline[i] *= shrink;

        var map = MapConceptLayout.CreateProcedural(outline);
        AddOffshoreIslands(map, parameters.OffshoreIslands, ref random);
        return map;
    }

    // =========================================================
    private static FastNoiseLite MakeNoise(int seed, float frequency, int octaves) =>
        new()
        {
            Seed = seed,
            NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth,
            Frequency = frequency,
            FractalOctaves = octaves
        };

    // =========================================================
    public static float LandPercent(MapConceptLayout map)
    {
        float area = PolygonArea(map.Coast);
        foreach (Vector2[] island in map.Islets) area += PolygonArea(island);
        return 100f * area / (MapConceptLayout.MapWidth * MapConceptLayout.MapHeight);
    }

    // =========================================================
    private static float PolygonArea(Vector2[] polygon)
    {
        double sum = 0;
        for (int i = 0; i < polygon.Length; i++)
        {
            Vector2 a = polygon[i], b = polygon[(i + 1) % polygon.Length];
            sum += (double)a.X * b.Y - (double)b.X * a.Y;
        }
        return (float)Math.Abs(sum * 0.5);
    }
    #endregion

    #region Small offshore islands

    // =========================================================
    // Bounded retry loop avoids overlapping islands, clipped coastlines and
    // disconnected shapes that are accidentally positioned on the mainland.
    private static void AddOffshoreIslands(MapConceptLayout map, int wanted, ref SeedStream random)
    {
        var centers = new List<(Vector2 Position, float Radius)>();
        const int MaxAttempts = 900;
        for (int attempt = 0; attempt < MaxAttempts && centers.Count < wanted; attempt++)
        {
            Vector2 center = new(random.Range(-725f, 725f), random.Range(-460f, 460f));
            float radius = random.Range(12f, 30f);
            if (Mathf.Abs(center.X) + radius + 7 > SafeHalfWidth ||
                Mathf.Abs(center.Y) + radius + 7 > SafeHalfHeight ||
                map.IsOnLand(center)) continue;

            float distance = DistanceToPolygon(center, map.Coast);
            if (distance < radius + 21f || distance > 235f) continue;
            bool overlaps = false;
            foreach (var existing in centers)
                if (center.DistanceTo(existing.Position) < radius + existing.Radius + 25f)
                {
                    overlaps = true;
                    break;
                }
            if (overlaps) continue;

            var vertices = new Vector2[IsletSamples];
            float rotation = random.Range(-Mathf.Pi, Mathf.Pi);
            float ovalness = random.Range(0.70f, 1.28f);
            for (int i = 0; i < vertices.Length; i++)
            {
                float a = rotation + Mathf.Tau * i / vertices.Length;
                float r = radius * random.Range(0.80f, 1.17f);
                vertices[i] = center + new Vector2(Mathf.Cos(a) * r * ovalness,
                    Mathf.Sin(a) * r / ovalness);
            }

            // Check actual coastline, not just the centre or a bounding circle.
            bool valid = true;
            foreach (Vector2 point in vertices)
            {
                if (Mathf.Abs(point.X) > SafeHalfWidth || Mathf.Abs(point.Y) > SafeHalfHeight ||
                    map.IsOnLand(point) || DistanceToPolygon(point, map.Coast) < 12f)
                {
                    valid = false;
                    break;
                }
            }
            if (!valid) continue;
            centers.Add((center, radius));
            map.Islets.Add(vertices);
        }
    }

    // =========================================================
    private static float DistanceToPolygon(Vector2 point, Vector2[] polygon)
    {
        float best = float.MaxValue;
        for (int i = 0; i < polygon.Length; i++)
        {
            Vector2 a = polygon[i], b = polygon[(i + 1) % polygon.Length];
            Vector2 segment = b - a;
            float lengthSquared = segment.LengthSquared();
            float t = lengthSquared < 0.0001f ? 0f :
                Mathf.Clamp((point - a).Dot(segment) / lengthSquared, 0f, 1f);
            best = Mathf.Min(best, point.DistanceTo(a + segment * t));
        }
        return best;
    }
    #endregion

    #region Stable seed derivation

    // =========================================================
    // SplitMix64 gives independent full-seed-dependent random streams and
    // stable 32-bit seeds for FastNoiseLite, whose Seed property is int32.
    public static int DecorationSeed(long seed)
    {
        var random = new SeedStream(unchecked((ulong)seed) ^ 0xBB47E5C13A2D109FUL);
        return random.NextInt();
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
                ulong value = (_state += 0x9E3779B97F4A7C15UL);
                value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
                value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
                return value ^ (value >> 31);
            }
        }

        // =========================================================
        public int NextInt() => unchecked((int)Next64());

        // =========================================================
        public int NextInt(int minInclusive, int maxExclusive) =>
            minInclusive + (int)(Next64() % (uint)(maxExclusive - minInclusive));

        // =========================================================
        public float Range(float minimum, float maximum) =>
            minimum + (float)((Next64() >> 40) / 16777216.0) * (maximum - minimum);
    }
    #endregion
}
