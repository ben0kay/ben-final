// BW-MAP2.1: deterministic mountain ridges and a sampled 2D elevation field.
using System;
using System.Collections.Generic;
using Godot;

internal static class MapConceptMountainGenerator
{
    private const ulong RidgeSalt = 0xB5D8B9E23C1A74E1UL;
    private const int AttemptsPerRange = 110;

    #region Mountain placement

    // =========================================================
    public static void Populate(MapConceptLayout layout, MapConceptParameters parameters)
    {
        var rng = new MapConceptSeedStream(unchecked((ulong)parameters.Seed) ^ RidgeSalt);
        int wanted = rng.NextInt(parameters.MinMountainRegions, parameters.MaxMountainRegions + 1);
        Rect2 bounds = MapConceptGeometry.Bounds(layout.Coast);
        foreach (Vector2[] island in layout.Islets)
            bounds = bounds.Merge(MapConceptGeometry.Bounds(island));

        for (int number = 1; number <= wanted; number++)
        {
            ConceptMountainRidge? chosen = null;
            for (int attempt = 0; attempt < AttemptsPerRange; attempt++)
            {
                Vector2 center = new(rng.Range(bounds.Position.X, bounds.End.X),
                    rng.Range(bounds.Position.Y, bounds.End.Y));
                if (!layout.IsOnLand(center) || layout.ShoreClearance(center) < 38f ||
                    TooCloseToExisting(center, layout.MountainRidges, 95f)) continue;

                float angle = rng.Range(0f, Mathf.Tau);
                Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 normal = new(-direction.Y, direction.X);
                float length = rng.Range(180f, 360f) *
                    (0.65f + parameters.MountainRidgeLength / 100f * 1.03f);
                float bend = rng.Range(-70f, 70f);
                float waviness = rng.Range(-15f, 15f) *
                    (0.35f + parameters.MountainRuggedness / 100f);
                var spine = new Vector2[9];
                int inland = 0;
                for (int i = 0; i < spine.Length; i++)
                {
                    float t = i / (float)(spine.Length - 1);
                    Vector2 p = center + direction * (t - 0.5f) * length +
                        normal * (bend * Mathf.Sin(t * Mathf.Pi) +
                            waviness * Mathf.Sin(3f * Mathf.Pi * t));
                    spine[i] = p;
                    if (layout.ShoreClearance(p) >= 18f && (i == 0 ||
                        layout.ShoreClearance(p.Lerp(spine[i - 1], 0.5f)) >= 18f)) inland++;
                }

                // Favour ranges that remain inland; no unbounded retries.
                if (inland < spine.Length) continue;
                float width = rng.Range(31f, 61f) *
                    (0.70f + parameters.MountainCoverage / 100f * 1.25f);
                float strength = rng.Range(0.80f, 0.99f);
                chosen = new ConceptMountainRidge(number, spine, width, strength)
                {
                    DetailSeed = unchecked(parameters.Seed ^
                        (long)(0x6347A5B1E3D29C8FUL + (ulong)number * 0x9E3779B97F4A7C15UL))
                };
                break;
            }

            if (chosen == null) continue;
            layout.MountainRidges.Add(chosen);
            layout.Regions.Add(new ConceptRegion(ConceptRegionKind.Highlands,
                number, Ribbon(chosen.Centreline, chosen.HalfWidth),
                26 + Mathf.RoundToInt(parameters.MountainCoverage * 0.23f)));
        }

        // An island always receives a lowland elevation field, even when the user
        // asks for zero mountains or some candidate ridge placements are rejected.
        BuildElevation(layout, parameters);
    }

    // =========================================================
    private static bool TooCloseToExisting(Vector2 center,
        List<ConceptMountainRidge> existing, float separation)
    {
        foreach (ConceptMountainRidge ridge in existing)
            if (center.DistanceTo(ridge.Centreline[ridge.Centreline.Length / 2]) < separation)
                return true;
        return false;
    }

    // =========================================================
    private static Vector2[] Ribbon(Vector2[] spine, float width)
    {
        var polygon = new Vector2[spine.Length * 2];
        for (int i = 0; i < spine.Length; i++)
        {
            float t = i / (float)(spine.Length - 1);
            Vector2 direction = (spine[Mathf.Min(i + 1, spine.Length - 1)] -
                spine[Mathf.Max(0, i - 1)]).Normalized();
            Vector2 normal = new(-direction.Y, direction.X);
            float taper = 0.58f + 0.42f * Mathf.Sin(t * Mathf.Pi);
            polygon[i] = spine[i] + normal * width * taper;
            polygon[polygon.Length - 1 - i] = spine[i] - normal * width * taper;
        }
        return polygon;
    }
    #endregion

    #region Elevation

    // =========================================================
    private static void BuildElevation(MapConceptLayout layout, MapConceptParameters settings)
    {
        var noiseSeeds = new MapConceptSeedStream(unchecked((ulong)settings.Seed) ^
            0x42C98B17E6D35AF0UL);
        using var plains = Noise(noiseSeeds.NextInt(), 0.006f, 3);
        using var rugged = Noise(noiseSeeds.NextInt(), 0.021f, 3);
        var elevation = new MapConceptElevationGrid();
        float roughness = settings.MountainRuggedness / 100f;

        for (int y = 0; y < MapConceptElevationGrid.Rows; y++)
        for (int x = 0; x < MapConceptElevationGrid.Columns; x++)
        {
            Vector2 point = MapConceptElevationGrid.Origin +
                new Vector2(x * MapConceptElevationGrid.Step.X,
                    y * MapConceptElevationGrid.Step.Y);
            if (!layout.IsOnLand(point))
            {
                elevation.Set(x, y, 0f);
                continue;
            }

            // Coastal lowlands taper to zero at sea level.
            float coastFactor = Mathf.SmoothStep(0f, 1f,
                Mathf.Clamp(layout.ShoreClearance(point) / 65f, 0f, 1f));
            float baseHeight = (0.15f + plains.GetNoise2D(point.X, point.Y) * 0.075f) *
                coastFactor;
            float mountainHeight = 0f;
            foreach (ConceptMountainRidge ridge in layout.MountainRidges)
            {
                float distance = DistanceToSpine(point, ridge.Centreline);
                float spread = ridge.HalfWidth * 1.38f;
                float falloff = Mathf.Exp(-0.5f * distance * distance / (spread * spread));
                float variation = rugged.GetNoise2D(point.X, point.Y);
                float summit = (0.74f + variation * roughness * 0.23f) * ridge.Strength;
                mountainHeight = Mathf.Max(mountainHeight, summit * falloff);
            }

            float foothills = Mathf.Clamp(mountainHeight * 0.25f +
                plains.GetNoise2D(point.X + 930f, point.Y) * 0.025f, 0f, 0.25f);
            float height = (baseHeight + mountainHeight + foothills) *
                Mathf.Lerp(0.58f, 1f, coastFactor);
            elevation.Set(x, y, height);
        }

        layout.Elevation = elevation;
    }

    // =========================================================
    private static float DistanceToSpine(Vector2 point, Vector2[] spine)
    {
        float nearest = float.MaxValue;
        for (int i = 0; i < spine.Length - 1; i++)
            nearest = Mathf.Min(nearest, DistanceToSegment(point, spine[i], spine[i + 1]));
        return nearest;
    }

    // =========================================================
    private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = ab.LengthSquared() < 0.0001f ? 0f :
            Mathf.Clamp((p - a).Dot(ab) / ab.LengthSquared(), 0f, 1f);
        return p.DistanceTo(a + ab * t);
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
    #endregion
}
