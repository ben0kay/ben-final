// BW-MAP2.2A: local 4096 raster carve and water classification, never per-frame.
using System;
using Godot;

internal static class MapConceptWaterCarver
{
    public const byte Land = 0;
    public const byte Ocean = 1;
    public const byte River = 2;
    public const byte Tributary = 3;

    // =========================================================
    public static void Apply(MapConceptMasterElevation master, MapConceptLayout layout)
    {
        // Single linear baseline pass: zero-elevation ocean remains saltwater.
        for (int i = 0; i < master.Heights.Length; i++)
            if (master.Heights[i] == 0) master.WaterMask[i] = Ocean;

        foreach (ConceptWaterway stream in layout.Waterways)
        {
            if (stream.Kind == ConceptWaterKind.OceanInlet) continue;
            byte kind = stream.Kind == ConceptWaterKind.River ? River : Tributary;
            // Exactly the same continuous curved centreline as the F6 drawing.
            MapConceptWaterSample[] samples = MapConceptWaterGeometry.Smooth(stream);
            for (int i = 0; i < samples.Length - 1; i++)
                CarveSegment(master, samples[i].Position, samples[i + 1].Position,
                    samples[i].HalfWidth, samples[i + 1].HalfWidth,
                    samples[i].SurfaceLevel, samples[i + 1].SurfaceLevel, kind);
        }
    }

    // =========================================================
    // Only touches the AABB surrounding a river segment, with a modest bank
    // transition. Keeps the existing high-resolution mountain surface intact.
    private static void CarveSegment(MapConceptMasterElevation master,
        Vector2 a, Vector2 b, float widthA, float widthB,
        float surfaceA, float surfaceB, byte kind)
    {
        const int side = MapConceptMasterElevation.Resolution;
        float step = MapConceptMasterElevation.Step;
        Vector2 origin = MapConceptMasterElevation.Origin;
        float rim = Mathf.Max(widthA, widthB) + 5f;
        int minX = Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(a.X, b.X) - rim - origin.X) / step), 0, side - 1);
        int maxX = Mathf.Clamp(Mathf.CeilToInt((Mathf.Max(a.X, b.X) + rim - origin.X) / step), 0, side - 1);
        int minY = Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(a.Y, b.Y) - rim - origin.Y) / step), 0, side - 1);
        int maxY = Mathf.Clamp(Mathf.CeilToInt((Mathf.Max(a.Y, b.Y) + rim - origin.Y) / step), 0, side - 1);
        Vector2 ab = b - a;
        float squared = ab.LengthSquared();
        if (squared <= 0.0001f) return;

        for (int y = minY; y <= maxY; y++)
        for (int x = minX; x <= maxX; x++)
        {
            int idx = y * side + x;
            if (master.Heights[idx] == 0) continue; // Do not paint freshwater over sea.
            Vector2 p = origin + new Vector2(x * step, y * step);
            float t = Mathf.Clamp((p - a).Dot(ab) / squared, 0f, 1f);
            float radius = Mathf.Lerp(widthA, widthB, t);
            float distance = p.DistanceTo(a + ab * t);
            float transition = radius + 4f;
            if (distance > transition) continue;

            float surface = Mathf.Lerp(surfaceA, surfaceB, t);
            float targetBed = Mathf.Max(0.002f, surface - 0.007f);
            float current = master.Heights[idx] / (float)ushort.MaxValue;
            // Keep the entire classified river channel below its water surface;
            // only the dry banks should fade gradually into original terrain.
            float bankBlend = distance <= radius ? 1f :
                1f - Mathf.SmoothStep(radius, transition, distance);
            float adjusted = Mathf.Min(current, Mathf.Lerp(current, targetBed, bankBlend));
            master.Heights[idx] = (ushort)Mathf.RoundToInt(adjusted * ushort.MaxValue);
            if (distance <= radius && master.WaterMask[idx] != Ocean)
            {
                // Main river wins when tributaries overlap at a junction.
                if (kind == River || master.WaterMask[idx] != River)
                    master.WaterMask[idx] = kind;
            }
        }
    }
}
