// MAP2.2A water centreline geometry shared by the visual painter and 4096 carving.
// Cubic interpolation keeps every original node, including river/tributary junctions.
using System;
using Godot;

internal readonly record struct MapConceptWaterSample(
    Vector2 Position, float HalfWidth, float SurfaceLevel);

internal static class MapConceptWaterGeometry
{
    private const int SamplesPerSegment = 8;

    // =========================================================
    public static MapConceptWaterSample[] Smooth(ConceptWaterway water)
    {
        int segments = water.Points.Length - 1;
        if (segments < 1) return Array.Empty<MapConceptWaterSample>();
        var samples = new MapConceptWaterSample[segments * SamplesPerSegment + 1];
        int output = 0;
        for (int i = 0; i < segments; i++)
        {
            Vector2 p0 = water.Points[Math.Max(0, i - 1)];
            Vector2 p1 = water.Points[i];
            Vector2 p2 = water.Points[i + 1];
            Vector2 p3 = water.Points[Math.Min(water.Points.Length - 1, i + 2)];
            for (int step = 0; step < SamplesPerSegment; step++)
            {
                float t = step / (float)SamplesPerSegment;
                Vector2 position = Curve(p0, p1, p2, p3, t);
                float radius = Mathf.Lerp(water.HalfWidths[i], water.HalfWidths[i + 1], t);
                float level = Mathf.Lerp(water.SurfaceLevels[i], water.SurfaceLevels[i + 1], t);
                samples[output++] = new MapConceptWaterSample(position, radius, level);
            }
        }
        int last = water.Points.Length - 1;
        samples[output] = new MapConceptWaterSample(water.Points[last],
            water.HalfWidths[last], water.SurfaceLevels[last]);
        return samples;
    }

    // =========================================================
    private static Vector2 Curve(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t)
    {
        float t2 = t * t, t3 = t2 * t;
        return 0.5f * ((2f * b) + (c - a) * t +
            (2f * a - 5f * b + 4f * c - d) * t2 +
            (-a + 3f * b - 3f * c + d) * t3);
    }
}
