// Vector channel subtraction preserves thin rivers without increasing the whole
// coastline sampling grid. Resulting shore polygons drive drawing AND the master.
using System;
using System.Collections.Generic;
using Godot;

internal static class MapConceptWaterCutout
{
    // =========================================================
    public static bool TryApply(List<Vector2[]> land, ConceptWaterway water,
        out List<Vector2[]> result)
    {
        Vector2[] ribbon = Ribbon(water);
        result = new List<Vector2[]>();
        float before = 0f, after = 0f;
        foreach (Vector2[] component in land)
        {
            before += MapConceptGeometry.PolygonArea(component);
            foreach (Vector2[] piece in Geometry2D.ClipPolygons(component, ribbon))
            {
                // A sea-connected cut cannot create an enclosed WATER hole.
                // Reject unexpected topology instead of filling it as land.
                if (Geometry2D.IsPolygonClockwise(piece)) return false;
                if (piece.Length < 3) continue;
                float area = MapConceptGeometry.PolygonArea(piece);
                if (area < 8f) continue;
                after += area;
                result.Add(piece);
            }
        }
        // Thin rivers must not accidentally erase whole mainland regions.
        return result.Count > 0 && after > before * 0.92f && after < before - 1f;
    }

    // =========================================================
    private static Vector2[] Ribbon(ConceptWaterway water)
    {
        int count = water.Points.Length;
        var normals = new Vector2[count];
        var polygon = new List<Vector2>(count * 2 + 24);
        for (int i = 0; i < count; i++)
        {
            Vector2 tangent = (water.Points[Math.Min(count - 1, i + 1)] -
                water.Points[Math.Max(0, i - 1)]).Normalized();
            normals[i] = new Vector2(-tangent.Y, tangent.X);
            polygon.Add(water.Points[i] + normals[i] * water.HalfWidths[i]);
        }
        for (int i = 1; i <= 12; i++)
            polygon.Add(water.Points[^1] + normals[^1].Rotated(-Mathf.Pi * i / 12f) * water.HalfWidths[^1]);
        for (int i = count - 2; i >= 0; i--)
            polygon.Add(water.Points[i] - normals[i] * water.HalfWidths[i]);
        for (int i = 1; i < 12; i++)
            polygon.Add(water.Points[0] - normals[0].Rotated(-Mathf.Pi * i / 12f) * water.HalfWidths[0]);
        Vector2[] result = polygon.ToArray();
        if (Geometry2D.IsPolygonClockwise(result)) Array.Reverse(result);
        return result;
    }
}
