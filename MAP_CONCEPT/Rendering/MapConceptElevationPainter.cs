// MAP2.1: lightweight topographic contour strokes from the generated elevation grid.
using System;
using Godot;

internal sealed class MapConceptElevationPainter
{
    private readonly MapConceptArt _canvas;
    private readonly MapConceptElevationGrid _grid;

    public MapConceptElevationPainter(MapConceptArt canvas, MapConceptElevationGrid grid)
    {
        _canvas = canvas;
        _grid = grid;
    }

    // =========================================================
    // Rendering happens only when the map is redrawn; no per-frame loops.
    public void Draw()
    {
        ReadOnlySpan<float> levels = stackalloc float[] { 0.23f, 0.38f, 0.54f, 0.70f, 0.86f };
        Span<float> heights = stackalloc float[4];
        Span<Vector2> corners = stackalloc Vector2[4];
        Span<Vector2> crossings = stackalloc Vector2[4];
        Vector2 origin = MapConceptElevationGrid.Origin;
        Vector2 step = MapConceptElevationGrid.Step;

        foreach (float level in levels)
        {
            var ink = new Color(0.37f, 0.35f, 0.28f, level >= 0.7f ? 0.42f : 0.25f);
            for (int y = 0; y < MapConceptElevationGrid.Rows - 1; y++)
            for (int x = 0; x < MapConceptElevationGrid.Columns - 1; x++)
            {
                heights[0] = _grid.At(x, y);
                heights[1] = _grid.At(x + 1, y);
                heights[2] = _grid.At(x + 1, y + 1);
                heights[3] = _grid.At(x, y + 1);
                float lowest = Mathf.Min(Mathf.Min(heights[0], heights[1]), Mathf.Min(heights[2], heights[3]));
                float highest = Mathf.Max(Mathf.Max(heights[0], heights[1]), Mathf.Max(heights[2], heights[3]));
                if (level <= lowest || level >= highest) continue;

                Vector2 p = origin + new Vector2(x * step.X, y * step.Y);
                corners[0] = p;
                corners[1] = p + new Vector2(step.X, 0);
                corners[2] = p + step;
                corners[3] = p + new Vector2(0, step.Y);
                int count = 0;
                for (int edge = 0; edge < 4; edge++)
                {
                    int next = (edge + 1) % 4;
                    if ((heights[edge] >= level) == (heights[next] >= level)) continue;
                    float t = (level - heights[edge]) / (heights[next] - heights[edge]);
                    crossings[count++] = corners[edge].Lerp(corners[next], t);
                }
                if (count >= 2) _canvas.DrawLine(crossings[0], crossings[1], ink, 1.15f, true);
                if (count == 4) _canvas.DrawLine(crossings[2], crossings[3], ink, 1.15f, true);
            }
        }
    }
}
