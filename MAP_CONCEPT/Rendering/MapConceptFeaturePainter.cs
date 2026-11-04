// Draws waterways, roads, settlement symbols and the decorative map frame.
using System;
using Godot;

internal sealed class MapConceptFeaturePainter
{
    private readonly MapConceptArt Canvas;
    private readonly MapConceptLayout Layout;
    private static readonly Color Ink = new(0.16f, 0.25f, 0.23f);
    private static readonly Color Route = new(0.59f, 0.35f, 0.21f);

    public MapConceptFeaturePainter(MapConceptArt canvas, MapConceptLayout layout)
    {
        Canvas = canvas;
        Layout = layout;
    }

    // =========================================================
    public void Draw()
    {
        DrawWaterways();
        new MapConceptWaterPainter(Canvas, Layout).Draw();
        DrawRoads();
        DrawSites();
        DrawCompassAndFrame();
    }

    #region Draw phases

    // =========================================================
    private void DrawWaterways()
    {
        foreach (ConceptRoute path in Layout.Routes)
        {
            if (!path.IsRiver) continue;
            Vector2[] curve = MapConceptDrawingGeometry.SmoothOpen(path.Points, 2);
            Canvas.DrawPolyline(curve, new Color(0.23f, 0.42f, 0.42f, 0.55f), 7f, true);
            Canvas.DrawPolyline(curve, new Color(0.32f, 0.65f, 0.70f), 4.3f, true);
            Canvas.DrawPolyline(curve, new Color(0.73f, 0.89f, 0.86f, 0.78f), 1.4f, true);
        }
    }

    // =========================================================
    private void DrawRoads()
    {
        foreach (ConceptRoute path in Layout.Routes)
        {
            if (path.IsRiver) continue;
            Vector2[] curve = MapConceptDrawingGeometry.SmoothOpen(path.Points, 2);
            Canvas.DrawPolyline(curve, new Color(0.97f, 0.89f, 0.66f, 0.72f), 5f, true);
            for (int i = 0; i < curve.Length - 1; i++)
            {
                Vector2 from = curve[i], to = curve[i + 1];
                float length = from.DistanceTo(to);
                int parts = Math.Max(1, (int)Math.Ceiling(length / 7f));
                for (int j = 0; j < parts; j++)
                {
                    float t = (float)j / parts;
                    if ((i * 11 + j) % 3 == 2) continue;
                    Vector2 a = from.Lerp(to, t), b = from.Lerp(to, (j + 0.75f) / parts);
                    Canvas.DrawLine(a, b, new Color(Route.R, Route.G, Route.B, 0.69f), 1.3f, true);
                }
            }
        }
    }

    // =========================================================
    private void DrawSites()
    {
        foreach (ConceptSite site in Layout.Sites)
        {
            Vector2 p = site.Position;
            Canvas.DrawCircle(p, 13, new Color(0.96f, 0.89f, 0.70f, 0.86f));
            Canvas.DrawArc(p, 13, 0, Mathf.Tau, 24, new Color(0.41f, 0.32f, 0.23f, 0.78f), 1.6f, true);
            if (site.Kind == ConceptSiteKind.Airstrip)
            {
                Canvas.DrawRect(new Rect2(p.X - 14, p.Y - 4, 28, 8), new Color(0.31f, 0.37f, 0.36f), true);
                Canvas.DrawLine(p + new Vector2(-10, 0), p + new Vector2(10, 0), new Color(0.96f, 0.92f, 0.77f), 1.7f, true);
            }
            else if (site.Kind == ConceptSiteKind.Ruin)
            {
                Canvas.DrawRect(new Rect2(p.X - 6, p.Y - 5, 12, 10), new Color(0.63f, 0.56f, 0.45f));
                Canvas.DrawLine(p + new Vector2(-8, -7), p + new Vector2(7, 8), new Color(0.39f, 0.31f, 0.24f), 2);
            }
            else
            {
                Canvas.DrawRect(new Rect2(p.X - 6, p.Y - 4, 12, 10), new Color(0.66f, 0.56f, 0.39f));
                Canvas.DrawColoredPolygon(new[] { p + new Vector2(-9, -4), p + new Vector2(0, -12),
                    p + new Vector2(9, -4) }, new Color(0.54f, 0.26f, 0.18f));
                Canvas.DrawRect(new Rect2(p.X - 2, p.Y, 4, 6), new Color(0.30f, 0.29f, 0.22f));
            }
        }
    }

    // =========================================================
    private void DrawCompassAndFrame()
    {
        Vector2 p = new(737, -403);
        Canvas.DrawCircle(p, 48, new Color(0.91f, 0.92f, 0.82f, 0.30f));
        Canvas.DrawArc(p, 47, 0, Mathf.Tau, 48, Ink, 2f, true);
        Canvas.DrawArc(p, 35, 0, Mathf.Tau, 32, new Color(Ink.R, Ink.G, Ink.B, 0.6f), 1f, true);
        Canvas.DrawColoredPolygon(new[] { p + new Vector2(0,-54), p + new Vector2(-9,12),
            p + new Vector2(0,2) }, Ink);
        Canvas.DrawColoredPolygon(new[] { p + new Vector2(0,-54), p + new Vector2(9,12),
            p + new Vector2(0,2) }, new Color(0.85f, 0.89f, 0.83f));
        Canvas.DrawLine(p + new Vector2(-60,0), p + new Vector2(60,0), Ink, 1.4f);
        Canvas.DrawLine(p + new Vector2(0,-60), p + new Vector2(0,60), Ink, 1.4f);
        Canvas.DrawCircle(p, 6, Ink);
        // Decorative frame; controls and labels are separate CanvasLayer/Label nodes.
        Canvas.DrawRect(new Rect2(-791, -519, 1582, 1038), new Color(0.20f,0.34f,0.33f,0.65f), false, 2);
        Canvas.DrawRect(new Rect2(-782, -510, 1564, 1020), new Color(0.98f,0.97f,0.85f,0.52f), false, 1);
        Canvas.DrawLine(new Vector2(489,475), new Vector2(715,475), Ink, 1.5f);
        for (int i = 0; i <= 4; i++)
            Canvas.DrawLine(new Vector2(489 + i*56.5f, 470), new Vector2(489 + i*56.5f, 480), Ink, 1.5f);
    }
    #endregion
}
