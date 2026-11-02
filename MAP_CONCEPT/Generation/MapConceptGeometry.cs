// Shared polygon area, signed winding and bounds used by map generation and tracing.
using System;
using System.Collections.Generic;
using Godot;

internal static class MapConceptGeometry
{
    internal static float PolygonArea(Vector2[] polygon) => Mathf.Abs(SignedArea(polygon));

    internal static float SignedArea(IReadOnlyList<Vector2> polygon)
    {
        double twiceArea = 0;
        for (int i = 0; i < polygon.Count; i++)
        {
            Vector2 a = polygon[i], b = polygon[(i + 1) % polygon.Count];
            twiceArea += (double)a.X * b.Y - (double)b.X * a.Y;
        }
        return (float)(twiceArea * 0.5);
    }

    internal static Rect2 Bounds(Vector2[] points)
    {
        Vector2 low = points[0], high = points[0];
        foreach (Vector2 p in points) { low = low.Min(p); high = high.Max(p); }
        return new Rect2(low, high - low);
    }
}
