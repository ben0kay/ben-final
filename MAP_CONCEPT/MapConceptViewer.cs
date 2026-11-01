// Independent 2D map viewer: mouse-centred zoom, drag panning and deterministic concept export.
using System;
using Godot;

public partial class MapConceptViewer : Node2D
{
    private Camera2D _camera = null!;
    private MapConceptArt _art = null!;
    private SpinBox _seedField = null!;
    private CheckBox _labels = null!;
    private Label _status = null!;
    private Button _rebuild = null!, _reset = null!, _export = null!;
    private bool _dragging;
    private float _zoomMinimum = 0.30f, _zoomMaximum = 3.2f;

    #region Lifecycle

    // =========================================================
    public override void _Ready()
    {
        _camera = GetNode<Camera2D>("MapCamera");
        _art = GetNode<MapConceptArt>("MapArtwork");
        _seedField = GetNode<SpinBox>("UI/SidePanel/Contents/ScatterSeed");
        _labels = GetNode<CheckBox>("UI/SidePanel/Contents/ShowLabels");
        _status = GetNode<Label>("UI/SidePanel/Contents/Status");
        _rebuild = GetNode<Button>("UI/SidePanel/Contents/RebuildButton");
        _reset = GetNode<Button>("UI/SidePanel/Contents/ResetViewButton");
        _export = GetNode<Button>("UI/SidePanel/Contents/ExportButton");
        _seedField.Value = _art.ScatterSeed;
        _labels.ButtonPressed = true;
        _rebuild.Pressed += Rebuild;
        _reset.Pressed += ResetView;
        _export.Pressed += ExportBlueprint;
        _labels.Toggled += OnLabelsToggled;
        _camera.MakeCurrent();
        ResetView();
        _status.Text = "Layout: Greywake v1 | Scatter seed: 147\nAuthor-defined coastline, regions and sites.";
    }

    // =========================================================
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_rebuild)) _rebuild.Pressed -= Rebuild;
        if (GodotObject.IsInstanceValid(_reset)) _reset.Pressed -= ResetView;
        if (GodotObject.IsInstanceValid(_export)) _export.Pressed -= ExportBlueprint;
        if (GodotObject.IsInstanceValid(_labels)) _labels.Toggled -= OnLabelsToggled;
    }
    #endregion

    #region Navigation

    // =========================================================
    public override void _UnhandledInput(InputEvent input)
    {
        if (input is InputEventMouseButton mouse)
        {
            if (mouse.ButtonIndex == MouseButton.WheelUp || mouse.ButtonIndex == MouseButton.WheelDown)
            {
                if (!mouse.Pressed) return;
                Vector2 before = GetGlobalMousePosition();
                float factor = mouse.ButtonIndex == MouseButton.WheelUp ? 1.15f : 1f / 1.15f;
                float zoom = Mathf.Clamp(_camera.Zoom.X * factor, _zoomMinimum, _zoomMaximum);
                _camera.Zoom = new Vector2(zoom, zoom);
                // Keep the map point under the cursor stationary when zooming.
                _camera.GlobalPosition += before - GetGlobalMousePosition();
                GetViewport().SetInputAsHandled();
                return;
            }

            if (mouse.ButtonIndex == MouseButton.Left ||
                mouse.ButtonIndex == MouseButton.Middle || mouse.ButtonIndex == MouseButton.Right)
            {
                _dragging = mouse.Pressed;
                GetViewport().SetInputAsHandled();
                return;
            }
        }

        if (input is InputEventMouseMotion motion && _dragging)
        {
            _camera.Position -= motion.Relative / _camera.Zoom.X;
            GetViewport().SetInputAsHandled();
            return;
        }

        if (input is InputEventKey key && key.Pressed && !key.Echo)
        {
            if (key.PhysicalKeycode == Key.R) { ResetView(); GetViewport().SetInputAsHandled(); }
            else if (key.PhysicalKeycode == Key.L)
            {
                _labels.ButtonPressed = !_labels.ButtonPressed;
                GetViewport().SetInputAsHandled();
            }
        }
    }

    // =========================================================
    // Release a held pan even when the mouse is released over the UI panel.
    public override void _Input(InputEvent input)
    {
        if (input is InputEventMouseButton mouse && !mouse.Pressed &&
            (mouse.ButtonIndex == MouseButton.Left || mouse.ButtonIndex == MouseButton.Middle ||
                mouse.ButtonIndex == MouseButton.Right))
            _dragging = false;
    }

    // =========================================================
    private void ResetView()
    {
        Vector2 viewport = GetViewportRect().Size;
        float scale = Mathf.Min(viewport.X / (MapConceptLayout.MapWidth + 110f),
            viewport.Y / (MapConceptLayout.MapHeight + 100f));
        scale = Mathf.Clamp(scale, _zoomMinimum, 1.35f);
        _camera.Zoom = new Vector2(scale, scale);
        _camera.Position = Vector2.Zero;
    }
    #endregion

    #region Concept controls

    // =========================================================
    private void Rebuild()
    {
        int seed = Mathf.Clamp((int)_seedField.Value, 0, 999999);
        _art.RegenerateScatter(seed);
        _status.Text = $"Layout: Greywake v1 | Scatter seed: {seed}\n" +
            "Coast, zone shapes, rivers and sites remain fixed.";
    }

    // =========================================================
    private void OnLabelsToggled(bool show) => _art.SetLabelsVisible(show);

    // =========================================================
    // Export is a reference blueprint for later terrain integration, not 3D geometry.
    private void ExportBlueprint()
    {
        const string path = "user://map_concept_layout.json";
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            _status.Text = "Could not export blueprint to user://. See Godot Output.";
            GD.PushWarning("[MAP CONCEPT] Could not open blueprint export file.");
            return;
        }
        file.StoreString(_art.Layout.BlueprintJson(_art.ScatterSeed));
        file.Flush();
        _status.Text = "Blueprint saved to:\n" + ProjectSettings.GlobalizePath(path);
        GD.Print("[MAP CONCEPT] Exported authored map blueprint: " + ProjectSettings.GlobalizePath(path));
    }
    #endregion
}
