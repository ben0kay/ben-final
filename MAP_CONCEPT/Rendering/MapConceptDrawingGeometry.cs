// Pure vector/seeded illustration helpers shared by the small rendering modules.
using System;
using System.Collections.Generic;
using Godot;

internal static class MapConceptDrawingGeometry
{
    internal static float Range(Random rng, float minimum, float maximum) =>
        minimum + (float)rng.NextDouble() * (maximum - minimum);

    internal static Vector2 RandomPoint(Random rng, Rect2 bounds) =>
        new(Range(rng, bounds.Position.X, bounds.End.X), Range(rng, bounds.Position.Y, bounds.End.Y));

    // =========================================================
    internal static Rect2 Bounds(Vector2[] points)
    {
        Vector2 low = points[0], high = points[0];
        foreach (Vector2 p in points) { low = low.Min(p); high = high.Max(p); }
        return new Rect2(low, high - low);
    }

    // =========================================================
    internal static Vector2 PolygonCenter(Vector2[] points)
    {
        Vector2 sum = Vector2.Zero;
        foreach (Vector2 p in points) sum += p;
        return sum / points.Length;
    }

    // =========================================================
    internal static int StableHash(string value)
    {
        unchecked
        {
            int h = 216613626;
            foreach (char c in value) h = (h ^ c) * 16777619;
            return h & int.MaxValue;
        }
    }

    // =========================================================
    internal static Vector2[] Close(Vector2[] points)
    {
        var closed = new Vector2[points.Length + 1];
        Array.Copy(points, closed, points.Length);
        closed[^1] = points[0];
        return closed;
    }

    // =========================================================
    internal static Vector2[] SmoothClosed(Vector2[] source, int passes)
    {
        Vector2[] points = source;
        for (int pass = 0; pass < passes; pass++)
        {
            var result = new Vector2[points.Length * 2];
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 a = points[i], b = points[(i + 1) % points.Length];
                result[i * 2] = a.Lerp(b, 0.25f);
                result[i * 2 + 1] = a.Lerp(b, 0.75f);
            }
            points = result;
        }
        return points;
    }

    // =========================================================
    internal static Vector2[] SmoothOpen(Vector2[] source, int passes)
    {
        Vector2[] points = source;
        for (int pass = 0; pass < passes; pass++)
        {
            var result = new List<Vector2>(points.Length * 2) { points[0] };
            for (int i = 0; i < points.Length - 1; i++)
            {
                result.Add(points[i].Lerp(points[i + 1], 0.25f));
                result.Add(points[i].Lerp(points[i + 1], 0.75f));
            }
            result.Add(points[^1]);
            points = result.ToArray();
        }
        return points;
    }
}
