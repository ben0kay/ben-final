// Independent 2D island viewer: MAP2.0 procedural coastlines and authored preset.
using System;
using System.Globalization;
using Godot;

public partial class MapConceptViewer : Node2D
{
    [Export] public MapConceptSettings Settings { get; set; } = null!;

    private Camera2D _camera = null!;
    private MapConceptArt _art = null!;
    private LineEdit _seedField = null!;
    private OptionButton _mode = null!;
    private CheckBox _labels = null!;
    private Label _status = null!;
    private Label _coverageText = null!, _roughnessText = null!, _peninsulaText = null!, _isletsText = null!;
    private Label _complexityText = null!, _oceanText = null!;
    private Label _mountainMinText = null!, _mountainMaxText = null!,
        _mountainCoverageText = null!, _ruggednessText = null!, _ridgeLengthText = null!;
    private HSlider _coverage = null!, _roughness = null!, _peninsulas = null!, _islets = null!;
    private HSlider _complexity = null!, _ocean = null!;
    private HSlider _mountainMin = null!, _mountainMax = null!,
        _mountainCoverage = null!, _ruggedness = null!, _ridgeLength = null!;
    private Button _rebuild = null!, _randomSeed = null!, _reset = null!, _export = null!;
    private long _activeSeed;
    private MapConceptParameters? _activeParameters;
    private bool _dragging;
    private const float MinZoom = 0.30f, MaxZoom = 3.2f;
    private const string ControlRoot = "UI/SidePanel/PanelScroll/Contents/";

    #region Lifecycle

    // =========================================================
    public override void _Ready()
    {
        if (Settings == null)
        {
            GD.PushError("[MAP2.0B] Missing MapConceptMaster.tres resource.");
            return;
        }
        _camera = GetNode<Camera2D>("MapCamera");
        _art = GetNode<MapConceptArt>("MapArtwork");
        _mode = GetNode<OptionButton>(ControlRoot + "MapMode");
        _seedField = GetNode<LineEdit>(ControlRoot + "WorldSeed");
        _labels = GetNode<CheckBox>(ControlRoot + "ShowLabels");
        _status = GetNode<Label>(ControlRoot + "Status");
        _coverage = GetNode<HSlider>(ControlRoot + "CoverageSlider");
        _roughness = GetNode<HSlider>(ControlRoot + "RoughnessSlider");
        _peninsulas = GetNode<HSlider>(ControlRoot + "PeninsulaSlider");
        _islets = GetNode<HSlider>(ControlRoot + "IsletsSlider");
        _complexity = GetNode<HSlider>(ControlRoot + "ComplexitySlider");
        _ocean = GetNode<HSlider>(ControlRoot + "OceanSlider");
        _mountainMin = GetNode<HSlider>(ControlRoot + "MountainMinSlider");
        _mountainMax = GetNode<HSlider>(ControlRoot + "MountainMaxSlider");
        _mountainCoverage = GetNode<HSlider>(ControlRoot + "MountainCoverageSlider");
        _ruggedness = GetNode<HSlider>(ControlRoot + "MountainRuggednessSlider");
        _ridgeLength = GetNode<HSlider>(ControlRoot + "MountainRidgeLengthSlider");
        _coverageText = GetNode<Label>(ControlRoot + "CoverageLabel");
        _roughnessText = GetNode<Label>(ControlRoot + "RoughnessLabel");
        _peninsulaText = GetNode<Label>(ControlRoot + "PeninsulaLabel");
        _isletsText = GetNode<Label>(ControlRoot + "IsletsLabel");
        _complexityText = GetNode<Label>(ControlRoot + "ComplexityLabel");
        _oceanText = GetNode<Label>(ControlRoot + "OceanLabel");
        _mountainMinText = GetNode<Label>(ControlRoot + "MountainMinLabel");
        _mountainMaxText = GetNode<Label>(ControlRoot + "MountainMaxLabel");
        _mountainCoverageText = GetNode<Label>(ControlRoot + "MountainCoverageLabel");
        _ruggednessText = GetNode<Label>(ControlRoot + "MountainRuggednessLabel");
        _ridgeLengthText = GetNode<Label>(ControlRoot + "MountainRidgeLengthLabel");
        _rebuild = GetNode<Button>(ControlRoot + "RebuildButton");
        _randomSeed = GetNode<Button>(ControlRoot + "RandomSeedButton");
        _reset = GetNode<Button>(ControlRoot + "ResetViewButton");
        _export = GetNode<Button>(ControlRoot + "ExportButton");

        _mode.AddItem("Procedural Seed", 0);
        _mode.AddItem("Authored Preset (Island 1)", 1);
        _mode.Select(0);
        _seedField.Text = Settings.DefaultSeed.ToString(CultureInfo.InvariantCulture);
        _coverage.Value = Settings.LandCoverage;
        _roughness.Value = Settings.CoastRoughness;
        _peninsulas.Value = Settings.BaysAndPeninsulas;
        _islets.Value = Settings.OffshoreIslands;
        _complexity.Value = Settings.LandmassComplexity;
        _ocean.Value = Settings.OceanPenetration;
        _mountainMin.Value = Settings.MinMountainRegions;
        _mountainMax.Value = Settings.MaxMountainRegions;
        _mountainCoverage.Value = Settings.MountainCoverage;
        _ruggedness.Value = Settings.MountainRuggedness;
        _ridgeLength.Value = Settings.MountainRidgeLength;
        _labels.ButtonPressed = true;

        _mode.ItemSelected += OnModeSelected;
        _seedField.TextSubmitted += OnSeedSubmitted;
        _rebuild.Pressed += GenerateMap;
        _randomSeed.Pressed += GenerateRandomSeed;
        _reset.Pressed += ResetView;
        _export.Pressed += ExportBlueprint;
        _labels.Toggled += OnLabelsToggled;
        _coverage.ValueChanged += UpdateSliderLabels;
        _roughness.ValueChanged += UpdateSliderLabels;
        _peninsulas.ValueChanged += UpdateSliderLabels;
        _islets.ValueChanged += UpdateSliderLabels;
        _complexity.ValueChanged += UpdateSliderLabels;
        _ocean.ValueChanged += UpdateSliderLabels;
        _mountainMin.ValueChanged += OnMinMountainsChanged;
        _mountainMax.ValueChanged += OnMaxMountainsChanged;
        _mountainCoverage.ValueChanged += UpdateSliderLabels;
        _ruggedness.ValueChanged += UpdateSliderLabels;
        _ridgeLength.ValueChanged += UpdateSliderLabels;
        UpdateSliderLabels(0);
        _camera.MakeCurrent();
        ResetView();
        GenerateMap();
    }

    // =========================================================
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_mode)) _mode.ItemSelected -= OnModeSelected;
        if (GodotObject.IsInstanceValid(_seedField)) _seedField.TextSubmitted -= OnSeedSubmitted;
        if (GodotObject.IsInstanceValid(_rebuild)) _rebuild.Pressed -= GenerateMap;
        if (GodotObject.IsInstanceValid(_randomSeed)) _randomSeed.Pressed -= GenerateRandomSeed;
        if (GodotObject.IsInstanceValid(_reset)) _reset.Pressed -= ResetView;
        if (GodotObject.IsInstanceValid(_export)) _export.Pressed -= ExportBlueprint;
        if (GodotObject.IsInstanceValid(_labels)) _labels.Toggled -= OnLabelsToggled;
        if (GodotObject.IsInstanceValid(_coverage)) _coverage.ValueChanged -= UpdateSliderLabels;
        if (GodotObject.IsInstanceValid(_roughness)) _roughness.ValueChanged -= UpdateSliderLabels;
        if (GodotObject.IsInstanceValid(_peninsulas)) _peninsulas.ValueChanged -= UpdateSliderLabels;
        if (GodotObject.IsInstanceValid(_islets)) _islets.ValueChanged -= UpdateSliderLabels;
        if (GodotObject.IsInstanceValid(_complexity)) _complexity.ValueChanged -= UpdateSliderLabels;
        if (GodotObject.IsInstanceValid(_ocean)) _ocean.ValueChanged -= UpdateSliderLabels;
        if (GodotObject.IsInstanceValid(_mountainMin)) _mountainMin.ValueChanged -= OnMinMountainsChanged;
        if (GodotObject.IsInstanceValid(_mountainMax)) _mountainMax.ValueChanged -= OnMaxMountainsChanged;
        if (GodotObject.IsInstanceValid(_mountainCoverage)) _mountainCoverage.ValueChanged -= UpdateSliderLabels;
        if (GodotObject.IsInstanceValid(_ruggedness)) _ruggedness.ValueChanged -= UpdateSliderLabels;
        if (GodotObject.IsInstanceValid(_ridgeLength)) _ridgeLength.ValueChanged -= UpdateSliderLabels;
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
                float zoom = Mathf.Clamp(_camera.Zoom.X * factor, MinZoom, MaxZoom);
                _camera.Zoom = new Vector2(zoom, zoom);
                _camera.GlobalPosition += before - GetGlobalMousePosition();
                GetViewport().SetInputAsHandled();
                return;
            }
            if (mouse.ButtonIndex == MouseButton.Left || mouse.ButtonIndex == MouseButton.Middle ||
                mouse.ButtonIndex == MouseButton.Right)
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
        }
        else if (input is InputEventKey key && key.Pressed && !key.Echo)
        {
            if (key.PhysicalKeycode == Key.R)
            {
                ResetView();
                GetViewport().SetInputAsHandled();
            }
            else if (key.PhysicalKeycode == Key.L)
            {
                _labels.ButtonPressed = !_labels.ButtonPressed;
                GetViewport().SetInputAsHandled();
            }
        }
    }

    // =========================================================
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
        scale = Mathf.Clamp(scale, MinZoom, 1.35f);
        _camera.Zoom = new Vector2(scale, scale);
        _camera.Position = Vector2.Zero;
    }
    #endregion

    #region Generation and UI

    // =========================================================
    private void OnSeedSubmitted(string value) => GenerateMap();

    // =========================================================
    private void OnModeSelected(long index)
    {
        bool procedural = _mode.GetSelectedId() == 0;
        _coverage.Editable = procedural;
        _roughness.Editable = procedural;
        _peninsulas.Editable = procedural;
        _islets.Editable = procedural;
        _complexity.Editable = procedural;
        _ocean.Editable = procedural;
        _mountainMin.Editable = procedural;
        _mountainMax.Editable = procedural;
        _mountainCoverage.Editable = procedural;
        _ruggedness.Editable = procedural;
        _ridgeLength.Editable = procedural;
        _rebuild.Text = procedural ? "Generate island from seed" : "Reload authored preset";
        GenerateMap();
    }

    // =========================================================
    private void OnMinMountainsChanged(double value)
    {
        if (_mountainMin.Value > _mountainMax.Value)
            _mountainMax.Value = _mountainMin.Value;
        UpdateSliderLabels(value);
    }

    // =========================================================
    private void OnMaxMountainsChanged(double value)
    {
        if (_mountainMax.Value < _mountainMin.Value)
            _mountainMin.Value = _mountainMax.Value;
        UpdateSliderLabels(value);
    }

    // =========================================================
    private void UpdateSliderLabels(double value)
    {
        _coverageText.Text = $"Landmass size target: {(int)_coverage.Value}%";
        _roughnessText.Text = $"Coastline roughness: {(int)_roughness.Value}%";
        _peninsulaText.Text = $"Peninsula branching: {(int)_peninsulas.Value}%";
        _isletsText.Text = $"Offshore islands requested: {(int)_islets.Value}";
        _complexityText.Text = $"Landmass complexity: {(int)_complexity.Value}%";
        _oceanText.Text = $"Ocean penetration: {(int)_ocean.Value}%";
        _mountainMinText.Text = $"Minimum mountain ranges: {(int)_mountainMin.Value}";
        _mountainMaxText.Text = $"Maximum mountain ranges: {(int)_mountainMax.Value}";
        _mountainCoverageText.Text = $"Mountain coverage: {(int)_mountainCoverage.Value}%";
        _ruggednessText.Text = $"Mountain ruggedness: {(int)_ruggedness.Value}%";
        _ridgeLengthText.Text = $"Ridge length: {(int)_ridgeLength.Value}%";
    }

    // =========================================================
    private void GenerateRandomSeed()
    {
        // A full 64-bit seed is preserved as text, never a precision-losing SpinBox float.
        _seedField.Text = Random.Shared.NextInt64(100_000_000_000L, long.MaxValue)
            .ToString(CultureInfo.InvariantCulture);
        GenerateMap();
    }

    // =========================================================
    private void GenerateMap()
    {
        if (!long.TryParse(_seedField.Text.Trim(), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out long seed))
        {
            _status.Text = "Invalid world seed. Enter a signed 64-bit whole number.";
            return;
        }

        bool procedural = _mode.GetSelectedId() == 0;
        MapConceptLayout layout;
        MapConceptParameters? parameters = null;
        if (procedural)
        {
            parameters = Settings.Snapshot(seed, (int)_coverage.Value, (int)_roughness.Value,
                (int)_peninsulas.Value, (int)_islets.Value,
                (int)_complexity.Value, (int)_ocean.Value,
                (int)_mountainMin.Value, (int)_mountainMax.Value,
                (int)_mountainCoverage.Value, (int)_ruggedness.Value,
                (int)_ridgeLength.Value);
            layout = MapConceptGenerator.Generate(parameters.Value);
        }
        else layout = MapConceptLayout.CreateDefault();

        _activeSeed = seed;
        _activeParameters = parameters;
        _art.SetMapLayout(layout, MapConceptGenerator.DecorationSeed(seed));
        _art.SetLabelsVisible(_labels.ButtonPressed);

        _status.Text = procedural
            ? $"Seed {seed} | Coastline v{MapConceptGenerator.GeneratorVersion}\n" +
              $"Land: {MapConceptGenerator.LandPercent(layout):F1}% of map | " +
              $"offshore islands: {layout.Islets.Count}/{parameters.Value.OffshoreIslands}\n" +
              $"Mountains: {layout.MountainRidges.Count} (range " +
              $"{parameters.Value.MinMountainRegions}–{parameters.Value.MaxMountainRegions}) | " +
              $"Elevation: {MapConceptElevationGrid.Columns}×{MapConceptElevationGrid.Rows}\n" +
              "Lakes, freshwater rivers, forests and settlements come later."
            : $"Authored preset Island 1 | Decorative seed {seed}\n" +
              "Original forests, mountains, roads and sites preserved.";
        GD.Print($"[MAP2.1] {(procedural ? "Procedural island" : "Authored preset")} seed {seed}; " +
            $"land {MapConceptGenerator.LandPercent(layout):F1}%; islets {layout.Islets.Count}.");
    }

    // =========================================================
    private void OnLabelsToggled(bool show) => _art.SetLabelsVisible(show);

    // =========================================================
    // Export the currently displayed map, not uncommitted seed-field text.
    private void ExportBlueprint()
    {
        const string path = "user://map_concept_layout.json";
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            _status.Text = "Could not export blueprint to user://. See Godot Output.";
            GD.PushWarning("[MAP2.0B] Could not open map blueprint export.");
            return;
        }
        file.StoreString(_art.Layout.BlueprintJson(_activeSeed, _activeParameters));
        file.Flush();
        _status.Text = "Current map blueprint saved to:\n" + ProjectSettings.GlobalizePath(path);
        GD.Print("[MAP2.0B] Exported blueprint: " + ProjectSettings.GlobalizePath(path));
    }
    #endregion
}
