// Lightweight vector-cartography preview: renders authored regions without generating terrain.
using System;
using System.Collections.Generic;
using Godot;

public partial class MapConceptArt : Node2D
{
    public MapConceptLayout Layout { get; private set; } = MapConceptLayout.CreateDefault();
    public int ScatterSeed { get; private set; } = 147;

    private readonly List<Label> _mapLabels = new();
    private bool _labelsVisible = true;
    private static readonly Color Ink = new(0.16f, 0.25f, 0.23f);

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
    // Regenerate the label nodes only when changing maps, not while panning.
    public void SetMapLayout(MapConceptLayout layout, int decorationSeed)
    {
        if (!ReferenceEquals(Layout, layout)) Layout.MasterElevation?.Dispose();
        Layout = layout;
        ScatterSeed = decorationSeed;
        foreach (Label label in _mapLabels)
        {
            RemoveChild(label);
            label.QueueFree();
        }
        _mapLabels.Clear();
        MakeLabels();
        SetLabelsVisible(_labelsVisible);
        QueueRedraw();
    }

    // =========================================================
    public void SetLabelsVisible(bool show)
    {
        _labelsVisible = show;
        foreach (Label label in _mapLabels) label.Visible = show;
    }

    // =========================================================
    public override void _Draw()
    {
        new MapConceptNaturalPainter(this, Layout, ScatterSeed).Draw();
        new MapConceptFeaturePainter(this, Layout).Draw();
    }


    #region Labels

    // =========================================================
    private void MakeLabels()
    {
        AddMapLabel(Layout.IsProcedural ? "ISLAND CONCEPT" : "ISLAND 1",
            new Vector2(-320, -495), 32,
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
                Vector2 center = MapConceptDrawingGeometry.PolygonCenter(zone.Outline);
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
    #endregion
}
