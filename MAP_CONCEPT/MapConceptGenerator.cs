// BW-MAP2.0B coordinator: preserves the existing generation order and quality scoring.
using System;
using System.Collections.Generic;
using Godot;

public static class MapConceptGenerator
{
    public const int GeneratorVersion = 3;
    private const int MaxAttempts = 5;

    #region Generation

    public static MapConceptLayout Generate(MapConceptParameters requested)
    {
        MapConceptParameters parameters = requested.Validated();
        MapConceptLayout? best = null;
        float bestScore = float.MinValue;

        for (int attempt = 0; attempt < MaxAttempts; attempt++)
        {
            ulong state = unchecked((ulong)parameters.Seed) +
                unchecked((ulong)attempt) * 0x9E3779B97F4A7C15UL;
            using var field = new MapConceptLandField(parameters, state);
            float[] samples = MapConceptCoastlineContours.SampleGrid(field);
            List<Vector2[]> contours = MapConceptCoastlineContours.ExtractLandContours(samples, field);
            if (contours.Count == 0) continue;

            contours.Sort((a, b) => MapConceptGeometry.PolygonArea(b).CompareTo(MapConceptGeometry.PolygonArea(a)));
            Vector2[] coast = contours[0];
            float area = MapConceptGeometry.PolygonArea(coast);
            Rect2 bounds = MapConceptGeometry.Bounds(coast);
            float total = 0f;
            foreach (Vector2[] contour in contours) total += MapConceptGeometry.PolygonArea(contour);
            float fraction = area / Mathf.Max(total, 1f);
            float score = Mathf.Min(area / 160000f, 1f) * fraction;
            if (bounds.Size.X < 450f || bounds.Size.Y < 270f) score *= 0.45f;

            var candidate = MapConceptLayout.CreateProcedural(coast);
            // Natural fragments count toward the user's island limit.
            for (int i = 1; i < contours.Count && candidate.Islets.Count < parameters.OffshoreIslands; i++)
                if (MapConceptGeometry.PolygonArea(contours[i]) >= 650f &&
                    MapConceptGeometry.PolygonArea(contours[i]) < area * 0.23f)
                    candidate.Islets.Add(contours[i]);

            var extraRandom = new MapConceptSeedStream(state ^ 0xD81956A02D35803FUL);
            MapConceptOffshoreIslands.AddOffshoreIslands(candidate, parameters.OffshoreIslands, ref extraRandom);

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
        if (best != null)
        {
            MapConceptMountainGenerator.Populate(best, parameters);
            return best;
        }
        var safeCoast = new Vector2[96];
        for (int i = 0; i < safeCoast.Length; i++)
        {
            float angle = Mathf.Tau * i / safeCoast.Length;
            safeCoast[i] = new Vector2(Mathf.Cos(angle) * 350f,
                Mathf.Sin(angle) * 255f);
        }
        var fallback = MapConceptLayout.CreateProcedural(safeCoast);
        MapConceptMountainGenerator.Populate(fallback, parameters);
        return fallback;
    }

    // =========================================================
    public static float LandPercent(MapConceptLayout map)
    {
        float area = MapConceptGeometry.PolygonArea(map.Coast);
        foreach (Vector2[] island in map.Islets) area += MapConceptGeometry.PolygonArea(island);
        return area / (MapConceptLayout.MapWidth * MapConceptLayout.MapHeight) * 100f;
    }

    // =========================================================
    public static int DecorationSeed(long seed)
    {
        var rng = new MapConceptSeedStream(unchecked((ulong)seed) ^ 0xBB47E5C13A2D109FUL);
        return rng.NextInt();
    }
    #endregion
}
