// BW-MAP2.2A: ocean-fed river outlets, elevation-guided upstream routes and tributaries.
// Produces bounded, seeded data paths; master rasterization is owned by WaterCarver.
using System;
using System.Collections.Generic;
using Godot;

internal static class MapConceptWaterGenerator
{
    private const ulong RiverSalt = 0x71B2C59D8453EA10UL;

    #region River network

    // =========================================================
    public static void Populate(MapConceptLayout map, MapConceptParameters settings)
    {
        if (map.Elevation == null) return;
        var random = new MapConceptSeedStream(unchecked((ulong)settings.Seed) ^ RiverSalt);
        // Rendering and the 4096 master both use this smoothed shoreline.
        Vector2[] coast = MapConceptDrawingGeometry.SmoothClosed(map.Coast, 2);
        int riverNumber = 0, branchNumber = 0;
        // Snapshot avoids mutating the enumerated collection as rivers are added.
        foreach (ConceptWaterway inlet in map.Waterways.FindAll(w =>
            w.Kind == ConceptWaterKind.OceanInlet))
        {
            if (inlet.Kind != ConceptWaterKind.OceanInlet) continue;
            if (!TryRiverMouth(map, coast, inlet, out Vector2 inlandStart,
                out Vector2 shoreline, out Vector2 oceanOverlap, out Vector2 heading))
                continue;

            var fromMouth = TraceUpstream(map, coast, inlandStart, heading,
                Mathf.RoundToInt(Mathf.Lerp(15f, 70f, settings.RiverReach / 100f)),
                settings.RiverWinding, ref random);
            if (fromMouth.Count < 5) continue;

            // The final two samples cross the *verified* coast into the inlet.
            // Never jump from an arbitrary inland point to an unverified tip.
            fromMouth.Reverse();
            fromMouth.Add(shoreline);
            fromMouth.Add(oceanOverlap);
            var river = MakeWaterway(ConceptWaterKind.River, ++riverNumber,
                fromMouth, 3.8f, 7.5f, 0f, map.Elevation)
                with { ConnectedWaterwayId = inlet.Id };
            map.Waterways.Add(river);

            // Tribs join a major river, not random seaward coast points.
            int tributaries = random.NextInt(0, settings.MaxTributaries + 1);
            for (int b = 0; b < tributaries; b++)
            {
                if (river.Points.Length < 11) break;
                int junction = random.NextInt(river.Points.Length / 3,
                    Mathf.Max(river.Points.Length / 3 + 1, river.Points.Length * 3 / 4));
                Vector2 downstream = (river.Points[junction + 1] -
                    river.Points[junction - 1]).Normalized();
                Vector2 upstream = -downstream;
                float direction = (b % 2 == 0 ? 1f : -1f) *
                    random.Range(0.75f, 1.30f);
                Vector2 sideways = upstream.Rotated(direction);
                var branch = TraceUpstream(map, coast, river.Points[junction], sideways,
                    random.NextInt(10, 27), settings.RiverWinding, ref random);
                if (branch.Count < 5) continue;
                branch.Reverse();
                map.Waterways.Add(MakeWaterway(ConceptWaterKind.Tributary,
                    ++branchNumber, branch, 2.1f, 3.7f,
                    river.SurfaceLevels[junction], map.Elevation)
                    with { ConnectedWaterwayId = river.Id });
            }
        }
    }

    // =========================================================
    private static bool TryRiverMouth(MapConceptLayout map, Vector2[] coast,
        ConceptWaterway inlet,
        out Vector2 inlandStart, out Vector2 shoreline, out Vector2 oceanOverlap,
        out Vector2 heading)
    {
        Vector2 tip = inlet.Points[^1];
        inlandStart = shoreline = oceanOverlap = tip;
        // If the final inlet control point sits in discarded inland water,
        // locate its last genuine sea vertex and follow that local tangent.
        int seaIndex = inlet.Points.Length - 1;
        while (seaIndex >= 0 &&
            MapConceptLayout.PointInPolygon(inlet.Points[seaIndex], coast))
            seaIndex--;
        if (seaIndex < 1) { heading = Vector2.Zero; return false; }
        Vector2 sea = inlet.Points[seaIndex];
        Vector2 toward = inlet.Points[Math.Min(seaIndex + 1, inlet.Points.Length - 1)];
        heading = (toward - inlet.Points[seaIndex - 1]).Normalized();
        if (heading.LengthSquared() < 0.5f) return false;

        // Walk from existing inlet WATER to the FIRST mainland shoreline.
        // This is the confluence. A finite fan handles curved inlet heads.
        float[] angles = { 0f, -0.20f, 0.20f, -0.42f, 0.42f, -0.68f, 0.68f };
        foreach (float angle in angles)
        {
            Vector2 direction = heading.Rotated(angle);
            Vector2 previousSea = sea;
            for (float distance = 3f; distance <= 155f; distance += 3f)
            {
                Vector2 next = sea + direction * distance;
                if (!MapConceptLayout.PointInPolygon(next, coast))
                {
                    previousSea = next;
                    continue;
                }

                // Bisect the real land/water crossing for a gap-free join.
                Vector2 waterEdge = previousSea, landEdge = next;
                for (int refine = 0; refine < 8; refine++)
                {
                    Vector2 mid = (waterEdge + landEdge) * 0.5f;
                    if (MapConceptLayout.PointInPolygon(mid, coast))
                        landEdge = mid;
                    else waterEdge = mid;
                }
                Vector2 shorePoint = (waterEdge + landEdge) * 0.5f;
                Vector2 start = shorePoint + direction * 17f;
                Vector2 overlap = shorePoint - direction * 10f;
                // Avoid starting in a tiny peninsula or immediately re-entering sea.
                if (!MapConceptLayout.PointInPolygon(start, coast) ||
                    !MapConceptLayout.PointInPolygon(start + direction * 12f, coast) ||
                    MapConceptLayout.PointInPolygon(overlap, coast) ||
                    map.Elevation!.Sample(start) < 0.012f)
                    break;

                inlandStart = start;
                shoreline = shorePoint;
                oceanOverlap = overlap;
                heading = direction;
                return true;
            }
        }
        return false; // Never emit a visually disconnected river.
    }

    // =========================================================
    // =========================================================
    // Grow inland from the outlet towards gradually higher ground. This is a
    // heuristic valley walker, not a full filled-depression watershed model.
    private static List<Vector2> TraceUpstream(MapConceptLayout map,
        Vector2[] coast, Vector2 origin,
        Vector2 heading, int maxSteps, int winding, ref MapConceptSeedStream random)
    {
        var points = new List<Vector2>(maxSteps + 1) { origin };
        Vector2 direction = heading.Normalized(), position = origin;
        float previousHeight = map.Elevation!.Sample(origin);

        for (int step = 0; step < maxSteps; step++)
        {
            float best = float.NegativeInfinity;
            Vector2 chosen = Vector2.Zero, chosenDir = Vector2.Zero;
            float chosenHeight = 0f;
            const int Choices = 13;
            for (int option = 0; option < Choices; option++)
            {
                float deviation = (option - 6) / 6f * Mathf.Lerp(0.42f, 1.13f,
                    winding / 100f);
                Vector2 nextDir = direction.Rotated(deviation).Normalized();
                Vector2 candidate = position + nextDir * 14f;
                if (!MapConceptLayout.PointInPolygon(candidate, coast) ||
                    !MapConceptLayout.PointInPolygon(position.Lerp(candidate, 0.5f),
                        coast)) continue;
                float height = map.Elevation.Sample(candidate);
                if (height < 0.014f || height > 0.74f || height < previousHeight - 0.017f)
                    continue;

                bool loops = false;
                for (int old = 0; old < points.Count - 2; old++)
                    if (candidate.DistanceSquaredTo(points[old]) < 25f * 25f)
                        { loops = true; break; }
                if (loops) continue;

                float grade = Mathf.Clamp((height - previousHeight) * 110f, -1.5f, 2f);
                float score = grade + direction.Dot(nextDir) * 1.7f +
                    random.Range(-0.40f, 0.40f) +
                    Mathf.Clamp(height * 0.45f, 0f, 0.25f);
                if (score <= best) continue;
                best = score;
                chosen = candidate;
                chosenDir = nextDir;
                chosenHeight = height;
            }
            if (float.IsNegativeInfinity(best)) break;
            points.Add(chosen);
            position = chosen;
            direction = (direction * 0.35f + chosenDir * 0.65f).Normalized();
            previousHeight = chosenHeight;
        }
        return points;
    }

    // =========================================================
    // Heights are water-surface values along the path, always descending
    // from source to mouth; WaterCarver lowers the terrain beneath them.
    private static ConceptWaterway MakeWaterway(ConceptWaterKind kind, int number,
        List<Vector2> sourceToMouth, float headWidth, float mouthWidth,
        float downstreamLevel, MapConceptElevationGrid elevation)
    {
        int count = sourceToMouth.Count;
        var widths = new float[count];
        var levels = new float[count];
        float surface = downstreamLevel;
        for (int i = count - 1; i >= 0; i--)
        {
            float t = i / (float)Mathf.Max(1, count - 1);
            widths[i] = Mathf.Lerp(headWidth, mouthWidth, t);
            if (i != count - 1)
            {
                float floor = Mathf.Max(0.005f, elevation.Sample(sourceToMouth[i]) - 0.012f);
                surface = Mathf.Clamp(Mathf.Max(surface + 0.00035f,
                    Mathf.Min(floor, surface + 0.0045f)), 0f, 0.90f);
            }
            levels[i] = surface;
        }
        return new ConceptWaterway(kind, number, sourceToMouth.ToArray(), widths, levels);
    }
    #endregion
}
