// Bounded placement of extra offshore islets; preserves existing candidate filtering.
using System;
using System.Collections.Generic;
using Godot;

internal static class MapConceptOffshoreIslands
{
    #region Offshore islands and seed utility

    // =========================================================
    internal static void AddOffshoreIslands(MapConceptLayout map, int wanted, ref MapConceptSeedStream rng)
    {
        // Natural split-off landmasses were already added by contour extraction.
        const int MaxRetries = 750;
        var channels = new MapConceptWaterFootprint(map.Waterways);
        for (int attempt = 0; attempt < MaxRetries && map.Islets.Count < wanted; attempt++)
        {
            Vector2 center = new(rng.Range(-700f, 700f), rng.Range(-452f, 452f));
            float radius = rng.Range(16f, 32f);
            // Never use extra decorative islets to plug a planned channel.
            if (channels.Sample(center) >= -radius - 12f) continue;
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
            if (clear) map.Islets.Add(map.FinalShoreline ?
                MapConceptDrawingGeometry.SmoothClosed(ring, 2) : ring);
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

    #endregion
}

