// BW-MAP2.2A: show generated freshwater branches while retaining existing drawn ocean bays.
using Godot;

internal sealed class MapConceptWaterPainter
{
    private readonly MapConceptArt _canvas;
    private readonly MapConceptLayout _map;
    private static readonly Color Bank = new(0.18f, 0.36f, 0.38f, 0.63f);
    private static readonly Color Freshwater = new(0.32f, 0.64f, 0.76f, 0.97f);
    private static readonly Color Glint = new(0.75f, 0.89f, 0.90f, 0.65f);

    public MapConceptWaterPainter(MapConceptArt canvas, MapConceptLayout map)
    {
        _canvas = canvas;
        _map = map;
    }

    // =========================================================
    // The coastline mask already renders saltwater inlets; drawing their full
    // off-map excavating paths would be an incorrect painted sea river.
    public void Draw()
    {
        foreach (ConceptWaterway water in _map.Waterways)
        {
            if (water.Kind == ConceptWaterKind.OceanInlet) continue;
            for (int i = 0; i < water.Points.Length - 1; i++)
            {
                Vector2 a = water.Points[i], b = water.Points[i + 1];
                float width = water.HalfWidths[i] + water.HalfWidths[i + 1];
                _canvas.DrawLine(a, b, Bank, width + 3.5f, true);
                _canvas.DrawLine(a, b, Freshwater, width, true);
                _canvas.DrawLine(a, b, Glint, Mathf.Max(0.8f, width * 0.17f), true);
            }
        }
    }
}
