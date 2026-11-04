// Spatially indexed sampled corridors for bounded land-field evaluation.
// The index exists only during on-demand generation, never as a live manager.
using System.Collections.Generic;
using Godot;

internal sealed class MapConceptWaterFootprint
{
    private const float CellSize = 64f;
    private readonly record struct Segment(Vector2 A, Vector2 Delta,
        float LengthSquared, float WidthA, float WidthB);
    private readonly Dictionary<Vector2I, List<Segment>> _cells = new();

    // =========================================================
    public MapConceptWaterFootprint(IEnumerable<ConceptWaterway> waterways)
    {
        foreach (ConceptWaterway water in waterways)
        for (int i = 0; i < water.Points.Length - 1; i++)
        {
            Vector2 a = water.Points[i], b = water.Points[i + 1];
            float width = Mathf.Max(water.HalfWidths[i], water.HalfWidths[i + 1]);
            // Keep neighbouring contour samples in the same distance field;
            // also supports clearance checks around optional offshore islets.
            var margin = new Vector2(width + CellSize, width + CellSize);
            Vector2I low = Cell(a.Min(b) - margin), high = Cell(a.Max(b) + margin);
            var segment = new Segment(a, b - a, a.DistanceSquaredTo(b),
                water.HalfWidths[i], water.HalfWidths[i + 1]);
            for (int y = low.Y; y <= high.Y; y++)
            for (int x = low.X; x <= high.X; x++)
            {
                var key = new Vector2I(x, y);
                if (!_cells.TryGetValue(key, out var bucket))
                    _cells.Add(key, bucket = new List<Segment>());
                bucket.Add(segment);
            }
        }
    }

    // =========================================================
    // Positive inside a corridor; negative/outside is only a sign contract.
    public float Sample(Vector2 point)
    {
        if (!_cells.TryGetValue(Cell(point), out var segments)) return -1000f;
        float best = -1000f;
        foreach (Segment segment in segments)
        {
            float t = segment.LengthSquared <= 0.00001f ? 0f :
                Mathf.Clamp((point - segment.A).Dot(segment.Delta) / segment.LengthSquared, 0f, 1f);
            float width = Mathf.Lerp(segment.WidthA, segment.WidthB, t);
            best = Mathf.Max(best, width - point.DistanceTo(segment.A + segment.Delta * t));
        }
        return best;
    }

    // =========================================================
    private static Vector2I Cell(Vector2 p) => new(
        Mathf.FloorToInt(p.X / CellSize), Mathf.FloorToInt(p.Y / CellSize));
}
