// MAP2.2A-R3: route long, thin meanders across an already selected mainland.
// Narrow channels and modest pools are cut before mountains, never drawn on top.
using System;
using System.Collections.Generic;
using Godot;

internal static class MapConceptWaterGenerator
{
    private const ulong WaterSalt = 0x71B2C59D8453EA10UL;

    // =========================================================
    public static MapConceptLayout Populate(MapConceptLayout mainland, MapConceptParameters settings)
    {
        var random = new MapConceptSeedStream(unchecked((ulong)settings.Seed) ^ WaterSalt);
        var router = new MapConceptRiverRouter(mainland);
        var land = new List<Vector2[]> { MapConceptDrawingGeometry.SmoothClosed(mainland.Coast, 2) };
        foreach (Vector2[] island in mainland.Islets)
            land.Add(MapConceptDrawingGeometry.SmoothClosed(island, 2));
        var waterways = new List<ConceptWaterway>();
        float phase = random.Range(0f, Mathf.Tau);
        float reach = settings.RiverReach / 100f, winding = settings.RiverWinding / 100f;
        int branches = 0;
        float span = MapConceptGeometry.Bounds(mainland.Coast).Size.Length();
        for (int number = 1; number <= settings.MajorOceanInlets; number++)
        for (int attempt = 0; attempt < 8; attempt++)
        {
            float angle = phase + Mathf.Tau * (number - 1f) / settings.MajorOceanInlets +
                random.Range(-0.35f, 0.35f);
            Vector2 outward = Vector2.FromAngle(angle);
            Vector2 mouth = router.Mouth(outward);
            bool through = random.Range(0f, 1f) < 0.40f + reach * 0.35f;
            Vector2 exitDirection = Vector2.FromAngle(angle + Mathf.Pi + random.Range(-0.3f, 0.3f));
            Vector2 end = through ? router.Mouth(exitDirection) :
                mouth.Lerp(router.FarInterior(mouth), Mathf.Lerp(0.65f, 0.95f, reach));
            if (mouth.DistanceTo(end) < Mathf.Max(200f, span * 0.35f)) continue;
            List<Vector2> path = router.Meander(mouth - outward * 24f,
                through ? end - exitDirection * 24f : end, winding, ref random);
            path = RemoveLoops(path);
            if (path.Count < 12) continue;
            Vector2[] interior = SampleCurve(MapConceptDrawingGeometry.SmoothOpen(path.ToArray(), 3));
            var points = new List<Vector2>(interior.Length + 4) { mouth + outward * 24f, mouth };
            points.AddRange(interior);
            if (through) { points.Add(end); points.Add(end + exitDirection * 24f); }
            float length = PathLength(points);
            if (length < Mathf.Max(300f, span * 0.55f)) continue;

            float width = random.Range(2.8f, 4.0f);
            float pool = random.Range(9f, 12f);
            var widths = new float[points.Count];
            float distance = 0f;
            int split = 1;
            bool valid = true;
            for (int i = 0; i < points.Count; i++)
            {
                if (i > 0) distance += points[i].DistanceTo(points[i - 1]);
                float radius = width * (0.92f + 0.08f * Mathf.Sin(distance / 27f));
                // Only the immediate coastal mouth widens; no map-wide bay.
                radius = Mathf.Lerp(14f + settings.OceanPenetration * 0.04f, radius,
                    Mathf.Clamp(distance / 65f, 0f, 1f));
                float remaining = length - distance;
                if (through) radius = Mathf.Lerp(14f, radius, Mathf.Clamp(remaining / 65f, 0f, 1f));
                else radius = Mathf.Lerp(pool, radius, Mathf.Clamp(remaining / 18f, 0f, 1f));
                widths[i] = radius;
                if (distance < 65f) split = i;
                if (distance > 65f && (!through || remaining > 65f) &&
                    mainland.ShoreClearance(points[i]) < radius + 2f) valid = false;
            }
            if (!valid) continue;
            var whole = new ConceptWaterway(ConceptWaterKind.River, number,
                points.ToArray(), widths, new float[points.Count]) { CoastalChannel = true };
            if (!MapConceptWaterCutout.TryApply(land, whole, out var cut)) continue;
            land = cut;
            var inlet = Section(whole, 0, split, ConceptWaterKind.OceanInlet, number);
            var river = Section(whole, split, points.Count - 1, ConceptWaterKind.River, number) with
            {
                ConnectedWaterwayId = inlet.Id,
                Ending = through ? ConceptWaterEnding.OppositeCoast : ConceptWaterEnding.ConnectedBasin
            };
            waterways.Add(inlet);
            waterways.Add(river);

            // Small reconnecting side channels, rather than broad banana cuts.
            int wanted = random.NextInt(0, settings.MaxTributaries + 1);
            for (int b = 0; b < wanted; b++)
            {
                int a = Math.Min(points.Count - 2, split + 12 + b * 20);
                int z = Math.Min(points.Count - 2, a + 25);
                if (z - a < 12) continue;
                Vector2 tangent = (points[z] - points[a]).Normalized();
                Vector2 normal = new(-tangent.Y, tangent.X);
                var branchPoints = new Vector2[33];
                var branchWidths = new float[33];
                bool clear = true;
                float offset = (b % 2 == 0 ? 1f : -1f) * random.Range(15f, 24f);
                for (int j = 0; j < branchPoints.Length; j++)
                {
                    float t = j / (branchPoints.Length - 1f);
                    branchPoints[j] = points[a].Lerp(points[z], t) + normal * offset * Mathf.Sin(t * Mathf.Pi);
                    branchWidths[j] = width * 0.7f;
                    if (mainland.ShoreClearance(branchPoints[j]) < width + 4f) clear = false;
                }
                if (!clear) continue;
                var branch = new ConceptWaterway(ConceptWaterKind.Tributary, branches + 1,
                    branchPoints, branchWidths, new float[33])
                {
                    CoastalChannel = true, ConnectedWaterwayId = river.Id,
                    RejoinsWaterwayId = river.Id, Ending = ConceptWaterEnding.RejoinsChannel
                };
                if (!MapConceptWaterCutout.TryApply(land, branch, out cut)) continue;
                land = cut;
                waterways.Add(branch);
                branches++;
            }
            break;
        }
        land.Sort((a, b) => MapConceptGeometry.PolygonArea(b).CompareTo(MapConceptGeometry.PolygonArea(a)));
        var result = MapConceptLayout.CreateProcedural(land[0]);
        result.FinalShoreline = true;
        for (int i = 1; i < land.Count; i++) result.Islets.Add(land[i]);
        result.Waterways.AddRange(waterways);
        return result;
    }

    // =========================================================
    // Keep smooth curves without exporting/clipping thousands of redundant
    // near-identical vertices. This does not change the whole-map grid size.
    private static Vector2[] SampleCurve(Vector2[] source)
    {
        var result = new List<Vector2> { source[0] };
        for (int i = 1; i < source.Length - 1; i++)
            if (source[i].DistanceSquaredTo(result[^1]) >= 9f) result.Add(source[i]);
        result.Add(source[^1]);
        return result.ToArray();
    }

    // =========================================================
    private static List<Vector2> RemoveLoops(List<Vector2> source)
    {
        var path = new List<Vector2>();
        var seen = new Dictionary<Vector2, int>();
        foreach (Vector2 point in source)
        {
            if (seen.TryGetValue(point, out int previous))
            {
                for (int i = path.Count - 1; i > previous; i--) seen.Remove(path[i]);
                path.RemoveRange(previous + 1, path.Count - previous - 1);
            }
            else { seen.Add(point, path.Count); path.Add(point); }
        }
        return path;
    }

    // =========================================================
    private static float PathLength(List<Vector2> points)
    {
        float length = 0f;
        for (int i = 1; i < points.Count; i++) length += points[i - 1].DistanceTo(points[i]);
        return length;
    }

    // =========================================================
    private static ConceptWaterway Section(ConceptWaterway source, int start,
        int end, ConceptWaterKind kind, int number)
    {
        int count = end - start + 1;
        var points = new Vector2[count];
        var widths = new float[count];
        Array.Copy(source.Points, start, points, 0, count);
        Array.Copy(source.HalfWidths, start, widths, 0, count);
        return new ConceptWaterway(kind, number, points, widths, new float[count]) { CoastalChannel = true };
    }
}
