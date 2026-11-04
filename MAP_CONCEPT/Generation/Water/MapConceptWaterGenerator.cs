// MAP2.2A-R2: one sea-connected inlet/channel network shapes land BEFORE mountains.
// These are sea-level geographic corridors, not simulated freshwater drainage.
using System;
using System.Collections.Generic;
using Godot;

internal static class MapConceptWaterGenerator
{
    #region Coastal network

    // =========================================================
    public static List<ConceptWaterway> CreateNetwork(MapConceptParameters settings,
        Vector2 centre, Func<Vector2, float> baseLand, ref MapConceptSeedStream random)
    {
        var result = new List<ConceptWaterway>();
        float phase = random.Range(0f, Mathf.Tau);
        float penetration = settings.OceanPenetration / 100f;
        float reach = settings.RiverReach / 100f;
        float winding = settings.RiverWinding / 100f;
        int branchNumber = 0;
        for (int number = 1; number <= settings.MajorOceanInlets; number++)
        {
            float angle = phase + Mathf.Tau * (number - 1 + random.Range(-0.15f, 0.15f)) /
                settings.MajorOceanInlets;
            Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 side = new(-direction.Y, direction.X);
            bool through = reach >= 0.55f && random.Range(0f, 1f) < 0.35f;
            float end = through ? -1000f : Mathf.Lerp(100f, -220f, reach);
            float bend = random.Range(-110f, 110f) * (0.35f + penetration * 0.65f);
            float wiggle = random.Range(35f, 85f) * winding;
            float channelWidth = random.Range(17f, 24f);
            float basinWidth = random.Range(45f, 75f);
            // A basin ends inside the pre-water landmass, with room for banks.
            // Pull its endpoint toward the guaranteed central core if needed.
            if (!through)
                for (int retry = 0; retry < 6 &&
                    baseLand(centre + direction * end) < basinWidth + 24f; retry++)
                    end *= 0.5f;
            const int Count = 81;
            var points = new Vector2[Count];
            var widths = new float[Count];
            int junction = 1;
            for (int i = 0; i < Count; i++)
            {
                float t = i / (Count - 1f);
                float distance = Mathf.Lerp(1000f, end, t);
                float inland = Mathf.Clamp((650f - distance) / 420f, 0f, 1f);
                float envelope = Mathf.Sin(t * Mathf.Pi);
                points[i] = centre + direction * distance + side * envelope *
                    (bend + wiggle * Mathf.Sin(t * Mathf.Pi * (through ? 5f : 3f)));
                // A broad estuary tapers into the SAME channel, with gentle
                // width variation. All widths survive the 8-unit contour grid.
                float width = Mathf.Lerp(85f + penetration * 30f, channelWidth, inland);
                width += Mathf.Sin(t * 7f * Mathf.Pi) * 2.5f * inland;
                if (through)
                    width = Mathf.Lerp(width, 85f, Mathf.SmoothStep(0f, 1f,
                        Mathf.Clamp((-distance - 280f) / 420f, 0f, 1f)));
                else
                    width = Mathf.Lerp(width, basinWidth,
                        Mathf.SmoothStep(0f, 1f, Mathf.Clamp((t - 0.87f) / 0.13f, 0f, 1f)));
                widths[i] = width;
                if (distance > Mathf.Lerp(300f, 210f, penetration)) junction = i;
            }

            // Sample the WHOLE path once, then split only its labels/data.
            // Shared endpoint, tangent and width prevent an inlet/river seam.
            var whole = new ConceptWaterway(ConceptWaterKind.OceanInlet, number,
                points, widths, new float[Count]);
            MapConceptWaterSample[] samples = MapConceptWaterGeometry.Smooth(whole);
            int split = junction * MapConceptWaterGeometry.SamplesPerSegment;
            var inlet = Section(samples, 0, split, ConceptWaterKind.OceanInlet, number)
                with { CoastalChannel = true };
            var river = Section(samples, split, samples.Length - 1,
                ConceptWaterKind.River, number) with
                {
                    CoastalChannel = true, ConnectedWaterwayId = inlet.Id,
                    Ending = through ? ConceptWaterEnding.OppositeCoast : ConceptWaterEnding.ConnectedBasin
                };
            result.Add(inlet);
            result.Add(river);

            // Optional anabranches reconnect to the trunk at BOTH ends. The
            // enclosed land survives contour extraction as a delta-like island.
            int branches = random.NextInt(0, settings.MaxTributaries + 1);
            for (int b = 0; b < branches; b++)
            {
                int start = Math.Max(0, split - 64 + b * 12);
                int finish = Math.Min(samples.Length - 1, split + 90 + b * 12);
                Vector2 a = samples[start].Position, z = samples[finish].Position;
                Vector2 tangent = (z - a).Normalized();
                Vector2 normal = new(-tangent.Y, tangent.X);
                float offset = (b % 2 == 0 ? 1f : -1f) * random.Range(65f, 100f);
                var branchPoints = new Vector2[25];
                var branchWidths = new float[25];
                for (int j = 0; j < branchPoints.Length; j++)
                {
                    float t = j / (branchPoints.Length - 1f);
                    branchPoints[j] = a.Lerp(z, t) + normal * offset * Mathf.Sin(t * Mathf.Pi);
                    branchWidths[j] = Mathf.Lerp(samples[start].HalfWidth,
                        samples[finish].HalfWidth, t) * (1f - 0.35f * Mathf.Sin(t * Mathf.Pi));
                }
                var branch = new ConceptWaterway(ConceptWaterKind.Tributary, ++branchNumber,
                    branchPoints, branchWidths, new float[branchPoints.Length]);
                MapConceptWaterSample[] smooth = MapConceptWaterGeometry.Smooth(branch);
                result.Add(Section(smooth, 0, smooth.Length - 1,
                    ConceptWaterKind.Tributary, branchNumber) with
                    {
                        CoastalChannel = true, ConnectedWaterwayId = inlet.Id,
                        RejoinsWaterwayId = river.Id, Ending = ConceptWaterEnding.RejoinsChannel
                    });
            }
        }
        return result;
    }

    // =========================================================
    private static ConceptWaterway Section(MapConceptWaterSample[] samples,
        int start, int end, ConceptWaterKind kind, int number)
    {
        int count = end - start + 1;
        var points = new Vector2[count];
        var widths = new float[count];
        for (int i = 0; i < count; i++)
        {
            points[i] = samples[start + i].Position;
            widths[i] = samples[start + i].HalfWidth;
        }
        return new ConceptWaterway(kind, number, points, widths, new float[count]);
    }
    #endregion
}
