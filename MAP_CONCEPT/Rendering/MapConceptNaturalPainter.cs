// Illustrates sea, coastline, land regions, vegetation and mountain iconography.
using System;
using Godot;

internal sealed class MapConceptNaturalPainter
{
    private readonly MapConceptArt Canvas;
    private readonly MapConceptLayout Layout;
    private readonly int ScatterSeed;
    private static readonly Color Water = new(0.54f, 0.76f, 0.81f);
    private static readonly Color WaterInk = new(0.21f, 0.51f, 0.59f, 0.42f);
    private static readonly Color Parchment = new(0.85f, 0.84f, 0.68f);
    private static readonly Color Earth = new(0.43f, 0.40f, 0.29f);

    public MapConceptNaturalPainter(MapConceptArt canvas, MapConceptLayout layout, int scatterSeed)
    {
        Canvas = canvas;
        Layout = layout;
        ScatterSeed = scatterSeed;
    }

    // =========================================================
    public void Draw()
    {
        DrawWater();
        DrawLand();
        DrawRegionColours();
        DrawGroundMarks();
        DrawForests();
        DrawMountains();
    }

    #region Draw phases

    // =========================================================
    private void DrawWater()
    {
        Canvas.DrawRect(new Rect2(-3000, -2200, 6000, 4400), Water);
        var rng = new Random(0x3610 + ScatterSeed);
        for (int i = 0; i < 850; i++)
        {
            Vector2 pos = new(MapConceptDrawingGeometry.Range(rng, -1020, 1020), MapConceptDrawingGeometry.Range(rng, -710, 710));
            if (Layout.IsOnLand(pos)) continue;
            float length = MapConceptDrawingGeometry.Range(rng, 4, 18);
            Canvas.DrawLine(pos, pos + new Vector2(length, -1.5f),
                new Color(WaterInk.R, WaterInk.G, WaterInk.B, MapConceptDrawingGeometry.Range(rng, 0.15f, 0.45f)), 1f, true);
            if (i % 6 == 0)
                Canvas.DrawLine(pos + new Vector2(4, 4), pos + new Vector2(length * 0.78f, 3),
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
        Vector2[] coast = MapConceptDrawingGeometry.SmoothClosed(anchors, 2);
        // Sand-coloured coastal apron and two hand-inked strokes around each island.
        Canvas.DrawPolyline(MapConceptDrawingGeometry.Close(coast), new Color(0.98f, 0.94f, 0.76f, 0.84f), 17f, true);
        Canvas.DrawColoredPolygon(coast, Parchment);
        Canvas.DrawPolyline(MapConceptDrawingGeometry.Close(coast), new Color(0.54f, 0.47f, 0.32f, 0.55f), 4.5f, true);
        Canvas.DrawPolyline(MapConceptDrawingGeometry.Close(coast), new Color(0.94f, 0.91f, 0.69f), 1.6f, true);
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
            Canvas.DrawColoredPolygon(MapConceptDrawingGeometry.SmoothClosed(region.Outline, 2), shade);
        }
    }

    // =========================================================
    private void DrawGroundMarks()
    {
        var rng = new Random(0x2317 + ScatterSeed);
        for (int i = 0; i < 1400; i++)
        {
            Vector2 p = new(MapConceptDrawingGeometry.Range(rng, -695, 695), MapConceptDrawingGeometry.Range(rng, -480, 470));
            if (!Layout.IsOnLand(p)) continue;
            Canvas.DrawLine(p, p + new Vector2(MapConceptDrawingGeometry.Range(rng, 2, 7), MapConceptDrawingGeometry.Range(rng, -2, -0.5f)),
                new Color(Earth.R, Earth.G, Earth.B, MapConceptDrawingGeometry.Range(rng, 0.10f, 0.22f)), 0.75f, true);
        }

        foreach (ConceptRegion zone in Layout.Regions)
        {
            if (zone.Kind == ConceptRegionKind.Meadow || zone.Kind == ConceptRegionKind.Wetlands)
            {
                var local = new Random(MapConceptDrawingGeometry.StableHash(zone.Id) ^ ScatterSeed);
                Rect2 rect = MapConceptDrawingGeometry.Bounds(zone.Outline);
                for (int i = 0; i < zone.Density; i++)
                {
                    Vector2 pos = MapConceptDrawingGeometry.RandomPoint(local, rect);
                    if (!MapConceptLayout.PointInPolygon(pos, zone.Outline) || !Layout.IsOnLand(pos)) continue;
                    Color c = zone.Kind == ConceptRegionKind.Meadow
                        ? new Color(0.41f, 0.56f, 0.32f, 0.44f)
                        : new Color(0.20f, 0.43f, 0.36f, 0.65f);
                    Canvas.DrawLine(pos, pos + new Vector2(-2f, -5f), c, 1.3f, true);
                    Canvas.DrawLine(pos, pos + new Vector2(2f, -4f), c, 1.1f, true);
                    if (zone.Kind == ConceptRegionKind.Wetlands && i % 4 == 0)
                        Canvas.DrawLine(pos + new Vector2(-5, 5), pos + new Vector2(8, 5),
                            new Color(WaterInk.R, WaterInk.G, WaterInk.B, 0.35f), 1.2f, true);
                }
            }
        }
    }

    // =========================================================
    private void DrawForests()
    {
        foreach (ConceptRegion zone in Layout.Regions)
        {
            if (zone.Kind != ConceptRegionKind.PineForest && zone.Kind != ConceptRegionKind.MixedForest) continue;
            var rng = new Random(MapConceptDrawingGeometry.StableHash(zone.Id) ^ ScatterSeed);
            Rect2 bounds = MapConceptDrawingGeometry.Bounds(zone.Outline);
            for (int i = 0; i < zone.Density * 3; i++)
            {
                Vector2 pos = MapConceptDrawingGeometry.RandomPoint(rng, bounds);
                if (!MapConceptLayout.PointInPolygon(pos, zone.Outline) || !Layout.IsOnLand(pos)) continue;
                float radius = MapConceptDrawingGeometry.Range(rng, 3.5f, 7f);
                bool fir = zone.Kind == ConceptRegionKind.PineForest || rng.NextDouble() < 0.55;
                if (fir) DrawPine(pos, radius, rng);
                else DrawBroadleaf(pos, radius, rng);
            }
        }
    }

    // =========================================================
    private void DrawPine(Vector2 p, float r, Random rng)
    {
        Color tint = new Color(MapConceptDrawingGeometry.Range(rng, 0.12f, 0.24f), MapConceptDrawingGeometry.Range(rng, 0.34f, 0.47f),
            MapConceptDrawingGeometry.Range(rng, 0.20f, 0.31f), 0.88f);
        Canvas.DrawLine(p + new Vector2(0, r * 0.5f), p + new Vector2(0, -r * 1.4f),
            new Color(0.24f, 0.25f, 0.16f, 0.70f), 1.2f);
        Canvas.DrawColoredPolygon(new[] { p + new Vector2(0, -r * 1.5f),
            p + new Vector2(-r, r * 0.3f), p + new Vector2(r, r * 0.3f) }, tint);
        Canvas.DrawLine(p + new Vector2(-r * 0.3f, 0), p + new Vector2(0, -r),
            new Color(0.66f, 0.75f, 0.48f, 0.40f), 0.7f, true);
    }

    // =========================================================
    private void DrawBroadleaf(Vector2 p, float r, Random rng)
    {
        Canvas.DrawLine(p + new Vector2(0, r * 0.7f), p + new Vector2(0, -r * 0.7f),
            new Color(0.28f, 0.26f, 0.16f, 0.70f), 1.2f);
        Canvas.DrawCircle(p, r * 0.85f, new Color(0.13f, MapConceptDrawingGeometry.Range(rng, 0.34f, 0.50f), 0.23f, 0.91f));
        Canvas.DrawCircle(p + new Vector2(-r * 0.24f, -r * 0.27f), r * 0.35f,
            new Color(0.52f, 0.66f, 0.37f, 0.62f));
    }

    // =========================================================
    private void DrawMountains()
    {
        foreach (ConceptRegion zone in Layout.Regions)
        {
            if (zone.Kind != ConceptRegionKind.Highlands) continue;
            var rng = new Random(MapConceptDrawingGeometry.StableHash(zone.Id) ^ ScatterSeed);
            Rect2 bounds = MapConceptDrawingGeometry.Bounds(zone.Outline);
            for (int i = 0; i < zone.Density * 4; i++)
            {
                Vector2 p = MapConceptDrawingGeometry.RandomPoint(rng, bounds);
                if (!MapConceptLayout.PointInPolygon(p, zone.Outline) || !Layout.IsOnLand(p)) continue;
                float width = MapConceptDrawingGeometry.Range(rng, 9, 23), height = MapConceptDrawingGeometry.Range(rng, 14, 36);
                DrawMountain(p, width, height);
            }
        }
    }

    // =========================================================
    private void DrawMountain(Vector2 basePoint, float width, float height)
    {
        Vector2 peak = basePoint + new Vector2(-width * 0.12f, -height);
        Vector2 left = basePoint + new Vector2(-width, 0), right = basePoint + new Vector2(width, 0);
        Canvas.DrawColoredPolygon(new[] { left, peak, right },
            new Color(0.43f, 0.43f, 0.39f, 0.90f));
        Canvas.DrawColoredPolygon(new[] { peak, right, basePoint + new Vector2(0, -height * 0.23f) },
            new Color(0.72f, 0.70f, 0.61f, 0.94f));
        Canvas.DrawColoredPolygon(new[] { peak, peak + new Vector2(-width * 0.33f, height * 0.34f),
            peak + new Vector2(width * 0.21f, height * 0.33f) },
            new Color(0.96f, 0.95f, 0.84f, 0.94f));
        Canvas.DrawLine(left, peak, new Color(0.23f, 0.27f, 0.25f, 0.62f), 0.95f, true);
        Canvas.DrawLine(peak, right, new Color(0.23f, 0.27f, 0.25f, 0.54f), 0.85f, true);
    }
    #endregion
}
