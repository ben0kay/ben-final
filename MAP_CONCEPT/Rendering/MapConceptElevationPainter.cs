// MAP2.1: lightweight topographic contour strokes from the generated elevation grid.
using System;
using Godot;

internal sealed class MapConceptElevationPainter
{
    private readonly MapConceptArt _canvas;
    private readonly MapConceptElevationGrid _grid;
    private readonly MapConceptLayout _layout;

    public MapConceptElevationPainter(MapConceptArt canvas, MapConceptLayout layout)
    {
        _canvas = canvas;
        _layout = layout;
        _grid = layout.Elevation!;
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
                if (count >= 2) DrawOnLand(crossings[0], crossings[1], ink);
                if (count == 4) DrawOnLand(crossings[2], crossings[3], ink);
            }
        }
    }
    // =========================================================
    // The overview grid is coarser than thin channels. Clip its decorative
    // contour strokes to final land rather than letting them cross the water.
    private void DrawOnLand(Vector2 a, Vector2 b, Color colour)
    {
        for (int i = 0; i <= 8; i++)
            if (!_layout.IsOnLand(a.Lerp(b, i / 8f))) return;
        _canvas.DrawLine(a, b, colour, 1.15f, true);
    }

}

