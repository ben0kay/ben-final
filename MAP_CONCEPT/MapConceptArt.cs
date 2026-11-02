// Lightweight vector-cartography preview: renders authored regions without generating terrain.
using System;
using System.Collections.Generic;
using Godot;

public partial class MapConceptArt : Node2D
{
    public MapConceptLayout Layout { get; private set; } = MapConceptLayout.CreateDefault();
    public int ScatterSeed { get; private set; } = 147;

    private readonly List<Label> _mapLabels = new();
    private static readonly Color Water = new(0.54f, 0.76f, 0.81f);
    private static readonly Color WaterInk = new(0.21f, 0.51f, 0.59f, 0.42f);
    private static readonly Color Parchment = new(0.85f, 0.84f, 0.68f);
    private static readonly Color Earth = new(0.43f, 0.40f, 0.29f);
    private static readonly Color Forest = new(0.18f, 0.42f, 0.28f);
    private static readonly Color Ink = new(0.16f, 0.25f, 0.23f);
    private static readonly Color Route = new(0.59f, 0.35f, 0.21f);

    // =========================================================
    public override void _Ready()
    {
        MakeLabels();
        QueueRedraw();
    }

    // =========================================================
    public void RegenerateScatter(int seed)
    {
        ScatterSeed = seed;
        QueueRedraw();
    }

    // =========================================================
    public void SetLabelsVisible(bool show)
    {
        foreach (Label label in _mapLabels) label.Visible = show;
    }

    // =========================================================
    public override void _Draw()
    {
        DrawWater();
        DrawLand();
        DrawRegionColours();
        DrawGroundMarks();
        DrawForests();
        DrawMountains();
        DrawWaterways();
        DrawRoads();
        DrawSites();
        DrawCompassAndFrame();
    }

    #region Background and geography

    // =========================================================
    private void DrawWater()
    {
        DrawRect(new Rect2(-3000, -2200, 6000, 4400), Water);
        var rng = new Random(0x3610 + ScatterSeed);
        for (int i = 0; i < 850; i++)
        {
            Vector2 pos = new(Range(rng, -1020, 1020), Range(rng, -710, 710));
            if (Layout.IsOnLand(pos)) continue;
            float length = Range(rng, 4, 18);
            DrawLine(pos, pos + new Vector2(length, -1.5f),
                new Color(WaterInk.R, WaterInk.G, WaterInk.B, Range(rng, 0.15f, 0.45f)), 1f, true);
            if (i % 6 == 0)
                DrawLine(pos + new Vector2(4, 4), pos + new Vector2(length * 0.78f, 3),
                    new Color(0.93f, 0.98f, 0.94f, 0.20f), 1f, true);
        }
    }

    // =========================================================
    private void DrawLand()
    {
        DrawIsland(Layout.Coast);
        foreach (Vector2[] island in Layout.Islets) DrawIsland(island);
    }

    // =========================================================
    private void DrawIsland(Vector2[] anchors)
    {
        Vector2[] coast = SmoothClosed(anchors, 2);
        // Sand-coloured coastal apron and two hand-inked strokes around each island.
        DrawPolyline(Close(coast), new Color(0.98f, 0.94f, 0.76f, 0.84f), 17f, true);
        DrawColoredPolygon(coast, Parchment);
        DrawPolyline(Close(coast), new Color(0.54f, 0.47f, 0.32f, 0.55f), 4.5f, true);
        DrawPolyline(Close(coast), new Color(0.94f, 0.91f, 0.69f), 1.6f, true);
    }

    // =========================================================
    private void DrawRegionColours()
    {
        foreach (ConceptRegion region in Layout.Regions)
        {
            Color shade = region.Kind switch
            {
                ConceptRegionKind.PineForest => new Color(0.19f, 0.43f, 0.26f, 0.20f),
                ConceptRegionKind.MixedForest => new Color(0.24f, 0.49f, 0.31f, 0.19f),
                ConceptRegionKind.Highlands => new Color(0.52f, 0.50f, 0.44f, 0.22f),
                ConceptRegionKind.Wetlands => new Color(0.32f, 0.58f, 0.45f, 0.25f),
                _ => new Color(0.66f, 0.73f, 0.44f, 0.15f)
            };
            DrawColoredPolygon(SmoothClosed(region.Outline, 2), shade);
        }
    }

    // =========================================================
    private void DrawGroundMarks()
    {
        var rng = new Random(0x2317 + ScatterSeed);
        for (int i = 0; i < 1400; i++)
        {
            Vector2 p = new(Range(rng, -695, 695), Range(rng, -480, 470));
            if (!Layout.IsOnLand(p)) continue;
            DrawLine(p, p + new Vector2(Range(rng, 2, 7), Range(rng, -2, -0.5f)),
                new Color(Earth.R, Earth.G, Earth.B, Range(rng, 0.10f, 0.22f)), 0.75f, true);
        }

        foreach (ConceptRegion zone in Layout.Regions)
        {
            if (zone.Kind == ConceptRegionKind.Meadow || zone.Kind == ConceptRegionKind.Wetlands)
            {
                var local = new Random(StableHash(zone.Id) ^ ScatterSeed);
                Rect2 rect = Bounds(zone.Outline);
                for (int i = 0; i < zone.Density; i++)
                {
                    Vector2 pos = RandomPoint(local, rect);
                    if (!MapConceptLayout.PointInPolygon(pos, zone.Outline) || !Layout.IsOnLand(pos)) continue;
                    Color c = zone.Kind == ConceptRegionKind.Meadow
                        ? new Color(0.41f, 0.56f, 0.32f, 0.44f)
                        : new Color(0.20f, 0.43f, 0.36f, 0.65f);
                    DrawLine(pos, pos + new Vector2(-2f, -5f), c, 1.3f, true);
                    DrawLine(pos, pos + new Vector2(2f, -4f), c, 1.1f, true);
                    if (zone.Kind == ConceptRegionKind.Wetlands && i % 4 == 0)
                        DrawLine(pos + new Vector2(-5, 5), pos + new Vector2(8, 5),
                            new Color(WaterInk.R, WaterInk.G, WaterInk.B, 0.35f), 1.2f, true);
                }
            }
        }
    }
    #endregion

    #region Detailed cartography

    // =========================================================
    private void DrawForests()
    {
        foreach (ConceptRegion zone in Layout.Regions)
        {
            if (zone.Kind != ConceptRegionKind.PineForest && zone.Kind != ConceptRegionKind.MixedForest) continue;
            var rng = new Random(StableHash(zone.Id) ^ ScatterSeed);
            Rect2 bounds = Bounds(zone.Outline);
            for (int i = 0; i < zone.Density * 3; i++)
            {
                Vector2 pos = RandomPoint(rng, bounds);
                if (!MapConceptLayout.PointInPolygon(pos, zone.Outline) || !Layout.IsOnLand(pos)) continue;
                float radius = Range(rng, 3.5f, 7f);
                bool fir = zone.Kind == ConceptRegionKind.PineForest || rng.NextDouble() < 0.55;
                if (fir) DrawPine(pos, radius, rng);
                else DrawBroadleaf(pos, radius, rng);
            }
        }
    }

    // =========================================================
    private void DrawPine(Vector2 p, float r, Random rng)
    {
        Color tint = new Color(Range(rng, 0.12f, 0.24f), Range(rng, 0.34f, 0.47f),
            Range(rng, 0.20f, 0.31f), 0.88f);
        DrawLine(p + new Vector2(0, r * 0.5f), p + new Vector2(0, -r * 1.4f),
            new Color(0.24f, 0.25f, 0.16f, 0.70f), 1.2f);
        DrawColoredPolygon(new[] { p + new Vector2(0, -r * 1.5f),
            p + new Vector2(-r, r * 0.3f), p + new Vector2(r, r * 0.3f) }, tint);
        DrawLine(p + new Vector2(-r * 0.3f, 0), p + new Vector2(0, -r),
            new Color(0.66f, 0.75f, 0.48f, 0.40f), 0.7f, true);
    }

    // =========================================================
    private void DrawBroadleaf(Vector2 p, float r, Random rng)
    {
        DrawLine(p + new Vector2(0, r * 0.7f), p + new Vector2(0, -r * 0.7f),
            new Color(0.28f, 0.26f, 0.16f, 0.70f), 1.2f);
        DrawCircle(p, r * 0.85f, new Color(0.13f, Range(rng, 0.34f, 0.50f), 0.23f, 0.91f));
        DrawCircle(p + new Vector2(-r * 0.24f, -r * 0.27f), r * 0.35f,
            new Color(0.52f, 0.66f, 0.37f, 0.62f));
    }

    // =========================================================
    private void DrawMountains()
    {
        foreach (ConceptRegion zone in Layout.Regions)
        {
            if (zone.Kind != ConceptRegionKind.Highlands) continue;
            var rng = new Random(StableHash(zone.Id) ^ ScatterSeed);
            Rect2 bounds = Bounds(zone.Outline);
            for (int i = 0; i < zone.Density * 4; i++)
            {
                Vector2 p = RandomPoint(rng, bounds);
                if (!MapConceptLayout.PointInPolygon(p, zone.Outline) || !Layout.IsOnLand(p)) continue;
                float width = Range(rng, 9, 23), height = Range(rng, 14, 36);
                DrawMountain(p, width, height);
            }
        }
    }

    // =========================================================
    private void DrawMountain(Vector2 basePoint, float width, float height)
    {
        Vector2 peak = basePoint + new Vector2(-width * 0.12f, -height);
        Vector2 left = basePoint + new Vector2(-width, 0), right = basePoint + new Vector2(width, 0);
        DrawColoredPolygon(new[] { left, peak, right },
            new Color(0.43f, 0.43f, 0.39f, 0.90f));
        DrawColoredPolygon(new[] { peak, right, basePoint + new Vector2(0, -height * 0.23f) },
            new Color(0.72f, 0.70f, 0.61f, 0.94f));
        DrawColoredPolygon(new[] { peak, peak + new Vector2(-width * 0.33f, height * 0.34f),
            peak + new Vector2(width * 0.21f, height * 0.33f) },
            new Color(0.96f, 0.95f, 0.84f, 0.94f));
        DrawLine(left, peak, new Color(0.23f, 0.27f, 0.25f, 0.62f), 0.95f, true);
        DrawLine(peak, right, new Color(0.23f, 0.27f, 0.25f, 0.54f), 0.85f, true);
    }

    // =========================================================
    private void DrawWaterways()
    {
        foreach (ConceptRoute path in Layout.Routes)
        {
            if (!path.IsRiver) continue;
            Vector2[] curve = SmoothOpen(path.Points, 2);
            DrawPolyline(curve, new Color(0.23f, 0.42f, 0.42f, 0.55f), 7f, true);
            DrawPolyline(curve, new Color(0.32f, 0.65f, 0.70f), 4.3f, true);
            DrawPolyline(curve, new Color(0.73f, 0.89f, 0.86f, 0.78f), 1.4f, true);
        }
    }

    // =========================================================
    private void DrawRoads()
    {
        foreach (ConceptRoute path in Layout.Routes)
        {
            if (path.IsRiver) continue;
            Vector2[] curve = SmoothOpen(path.Points, 2);
            DrawPolyline(curve, new Color(0.97f, 0.89f, 0.66f, 0.72f), 5f, true);
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
                    DrawLine(a, b, new Color(Route.R, Route.G, Route.B, 0.69f), 1.3f, true);
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
            DrawCircle(p, 13, new Color(0.96f, 0.89f, 0.70f, 0.86f));
            DrawArc(p, 13, 0, Mathf.Tau, 24, new Color(0.41f, 0.32f, 0.23f, 0.78f), 1.6f, true);
            if (site.Kind == ConceptSiteKind.Airstrip)
            {
                DrawRect(new Rect2(p.X - 14, p.Y - 4, 28, 8), new Color(0.31f, 0.37f, 0.36f), true);
                DrawLine(p + new Vector2(-10, 0), p + new Vector2(10, 0), new Color(0.96f, 0.92f, 0.77f), 1.7f, true);
            }
            else if (site.Kind == ConceptSiteKind.Ruin)
            {
                DrawRect(new Rect2(p.X - 6, p.Y - 5, 12, 10), new Color(0.63f, 0.56f, 0.45f));
                DrawLine(p + new Vector2(-8, -7), p + new Vector2(7, 8), new Color(0.39f, 0.31f, 0.24f), 2);
            }
            else
            {
                DrawRect(new Rect2(p.X - 6, p.Y - 4, 12, 10), new Color(0.66f, 0.56f, 0.39f));
                DrawColoredPolygon(new[] { p + new Vector2(-9, -4), p + new Vector2(0, -12),
                    p + new Vector2(9, -4) }, new Color(0.54f, 0.26f, 0.18f));
                DrawRect(new Rect2(p.X - 2, p.Y, 4, 6), new Color(0.30f, 0.29f, 0.22f));
            }
        }
    }

    // =========================================================
    private void DrawCompassAndFrame()
    {
        Vector2 p = new(737, -403);
        DrawCircle(p, 48, new Color(0.91f, 0.92f, 0.82f, 0.30f));
        DrawArc(p, 47, 0, Mathf.Tau, 48, Ink, 2f, true);
        DrawArc(p, 35, 0, Mathf.Tau, 32, new Color(Ink.R, Ink.G, Ink.B, 0.6f), 1f, true);
        DrawColoredPolygon(new[] { p + new Vector2(0,-54), p + new Vector2(-9,12),
            p + new Vector2(0,2) }, Ink);
        DrawColoredPolygon(new[] { p + new Vector2(0,-54), p + new Vector2(9,12),
            p + new Vector2(0,2) }, new Color(0.85f, 0.89f, 0.83f));
        DrawLine(p + new Vector2(-60,0), p + new Vector2(60,0), Ink, 1.4f);
        DrawLine(p + new Vector2(0,-60), p + new Vector2(0,60), Ink, 1.4f);
        DrawCircle(p, 6, Ink);
        // Decorative frame; controls and labels are separate CanvasLayer/Label nodes.
        DrawRect(new Rect2(-791, -519, 1582, 1038), new Color(0.20f,0.34f,0.33f,0.65f), false, 2);
        DrawRect(new Rect2(-782, -510, 1564, 1020), new Color(0.98f,0.97f,0.85f,0.52f), false, 1);
        DrawLine(new Vector2(489,475), new Vector2(715,475), Ink, 1.5f);
        for (int i = 0; i <= 4; i++)
            DrawLine(new Vector2(489 + i*56.5f, 470), new Vector2(489 + i*56.5f, 480), Ink, 1.5f);
    }
    #endregion

    #region Labels and geometry utilities

    // =========================================================
    private void MakeLabels()
    {
        AddMapLabel("ISLAND 1", new Vector2(-320, -495), 32,
            new Color(0.24f, 0.27f, 0.23f), 380);
        AddMapLabel("OCEAN 1", new Vector2(515, 445), 19,
            new Color(0.16f, 0.39f, 0.48f), 260);
        AddMapLabel("N", new Vector2(713, -481), 24, Ink, 48);
        AddMapLabel("E", new Vector2(789, -417), 18, Ink, 48);
        foreach (ConceptRegion zone in Layout.Regions)
        {
            if (zone.Kind == ConceptRegionKind.Meadow || zone.Kind == ConceptRegionKind.Wetlands ||
                zone.Kind == ConceptRegionKind.Highlands || zone.Kind == ConceptRegionKind.PineForest ||
                zone.Kind == ConceptRegionKind.MixedForest)
            {
                Vector2 center = PolygonCenter(zone.Outline);
                int size = zone.Kind == ConceptRegionKind.Highlands ? 13 : 15;
                AddMapLabel(zone.Name, center + new Vector2(0, 5), size,
                    zone.Kind == ConceptRegionKind.Highlands ? new Color(0.28f, 0.27f, 0.24f) :
                    new Color(0.12f, 0.27f, 0.19f), 210);
            }
        }
        foreach (ConceptSite site in Layout.Sites)
            AddMapLabel(site.Name, site.Position + new Vector2(0, 19), 13,
                new Color(0.32f, 0.19f, 0.12f), 210);
    }

    // =========================================================
    private void AddMapLabel(string title, Vector2 position, int fontSize, Color colour, float width)
    {
        var label = new Label
        {
            Text = title,
            Position = position - new Vector2(width / 2, fontSize / 2),
            Size = new Vector2(width, fontSize * 1.8f),
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", colour);
        label.AddThemeColorOverride("font_outline_color", new Color(0.92f, 0.90f, 0.74f, 0.87f));
        label.AddThemeConstantOverride("outline_size", 3);
        AddChild(label);
        _mapLabels.Add(label);
    }

    // =========================================================
    private static float Range(Random rng, float minimum, float maximum) =>
        minimum + (float)rng.NextDouble() * (maximum - minimum);

    private static Vector2 RandomPoint(Random rng, Rect2 bounds) =>
        new(Range(rng, bounds.Position.X, bounds.End.X), Range(rng, bounds.Position.Y, bounds.End.Y));

    private static Rect2 Bounds(Vector2[] points)
    {
        Vector2 low = points[0], high = points[0];
        foreach (Vector2 p in points) { low = low.Min(p); high = high.Max(p); }
        return new Rect2(low, high - low);
    }

    private static Vector2 PolygonCenter(Vector2[] points)
    {
        Vector2 sum = Vector2.Zero;
        foreach (Vector2 p in points) sum += p;
        return sum / points.Length;
    }

    // =========================================================
    private static int StableHash(string value)
    {
        unchecked
        {
            int h = 216613626;
            foreach (char c in value) h = (h ^ c) * 16777619;
            return h & int.MaxValue;
        }
    }

    // =========================================================
    private static Vector2[] Close(Vector2[] points)
    {
        var closed = new Vector2[points.Length + 1];
        Array.Copy(points, closed, points.Length);
        closed[^1] = points[0];
        return closed;
    }

    // =========================================================
    // Chaikin smoothing is illustration-only; the authored polygon remains unchanged.
    private static Vector2[] SmoothClosed(Vector2[] source, int passes)
    {
        Vector2[] points = source;
        for (int pass = 0; pass < passes; pass++)
        {
            var result = new Vector2[points.Length * 2];
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 a = points[i], b = points[(i + 1) % points.Length];
                result[i * 2] = a.Lerp(b, 0.25f);
                result[i * 2 + 1] = a.Lerp(b, 0.75f);
            }
            points = result;
        }
        return points;
    }

    // =========================================================
    private static Vector2[] SmoothOpen(Vector2[] source, int passes)
    {
        Vector2[] points = source;
        for (int pass = 0; pass < passes; pass++)
        {
            var result = new List<Vector2>(points.Length * 2) { points[0] };
            for (int i = 0; i < points.Length - 1; i++)
            {
                result.Add(points[i].Lerp(points[i + 1], 0.25f));
                result.Add(points[i].Lerp(points[i + 1], 0.75f));
            }
            result.Add(points[^1]);
            points = result.ToArray();
        }
        return points;
    }
    #endregion
}
