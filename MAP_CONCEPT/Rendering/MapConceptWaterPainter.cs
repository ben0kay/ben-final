// BW-MAP2.2A water painter: seamless single-colour rivers in the ocean tint.
// Rounded centreline capsules match the shared 4096 riverbed carving geometry.
using Godot;

internal sealed class MapConceptWaterPainter
{
    private readonly MapConceptArt _canvas;
    private readonly MapConceptLayout _map;

    public MapConceptWaterPainter(MapConceptArt canvas, MapConceptLayout map)
    {
        _canvas = canvas;
        _map = map;
    }

    // =========================================================
    public void Draw()
    {
        // Saltwater is already represented by the *actual* coastline hole.
        // Freshwater extends across the shoreline into it, so there is no gap.
        foreach (ConceptWaterway water in _map.Waterways)
        {
            if (water.Kind == ConceptWaterKind.OceanInlet) continue;
            MapConceptWaterSample[] samples = MapConceptWaterGeometry.Smooth(water);
            if (samples.Length < 2) continue;

            Color ocean = MapConceptWaterPalette.Ocean;
            for (int i = 0; i < samples.Length - 1; i++)
            {
                float width = samples[i].HalfWidth + samples[i + 1].HalfWidth;
                _canvas.DrawLine(samples[i].Position, samples[i + 1].Position,
                    ocean, width, true);
            }

            // All joints/caps are circular, rather than thick, flat-ended
            // overlapping lines with visible rectangular colour seams.
            foreach (MapConceptWaterSample sample in samples)
                _canvas.DrawCircle(sample.Position, sample.HalfWidth, ocean);
        }
    }
}
