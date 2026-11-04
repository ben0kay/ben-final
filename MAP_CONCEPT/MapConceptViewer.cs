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
    private Label _inletsText = null!, _reachText = null!, _windingText = null!,
        _tributariesText = null!;
    private HSlider _coverage = null!, _roughness = null!, _peninsulas = null!, _islets = null!;
    private HSlider _complexity = null!, _ocean = null!;
    private HSlider _mountainMin = null!, _mountainMax = null!,
        _mountainCoverage = null!, _ruggedness = null!, _ridgeLength = null!;
    private HSlider _inlets = null!, _riverReach = null!, _riverWinding = null!,
        _tributaries = null!;
    private Button _rebuild = null!, _randomSeed = null!, _reset = null!, _export = null!;
    private Button _togglePanel = null!;
    private PanelContainer _sidePanel = null!;
    private long _activeSeed;
    private MapConceptParameters? _activeParameters;
    private bool _dragging;
    // Generous local-detail zoom without recomputing the map while navigating.
    private const float MinZoom = 0.18f, MaxZoom = 32f;
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
        _sidePanel = GetNode<PanelContainer>("UI/SidePanel");
        _togglePanel = GetNode<Button>("UI/PanelToggle");
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
        _inlets = GetNode<HSlider>(ControlRoot + "MajorInletsSlider");
        _riverReach = GetNode<HSlider>(ControlRoot + "RiverReachSlider");
        _riverWinding = GetNode<HSlider>(ControlRoot + "RiverWindingSlider");
        _tributaries = GetNode<HSlider>(ControlRoot + "TributariesSlider");
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
        _inletsText = GetNode<Label>(ControlRoot + "MajorInletsLabel");
        _reachText = GetNode<Label>(ControlRoot + "RiverReachLabel");
        _windingText = GetNode<Label>(ControlRoot + "RiverWindingLabel");
        _tributariesText = GetNode<Label>(ControlRoot + "TributariesLabel");
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
        _inlets.Value = Settings.MajorOceanInlets;
        _riverReach.Value = Settings.RiverReach;
        _riverWinding.Value = Settings.RiverWinding;
        _tributaries.Value = Settings.MaxTributaries;
        _labels.ButtonPressed = true;

        _mode.ItemSelected += OnModeSelected;
        _seedField.TextSubmitted += OnSeedSubmitted;
        _rebuild.Pressed += GenerateMap;
        _randomSeed.Pressed += GenerateRandomSeed;
        _reset.Pressed += ResetView;
        _export.Pressed += ExportBlueprint;
        _togglePanel.Pressed += TogglePanel;
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
        _inlets.ValueChanged += UpdateSliderLabels;
        _riverReach.ValueChanged += UpdateSliderLabels;
        _riverWinding.ValueChanged += UpdateSliderLabels;
        _tributaries.ValueChanged += UpdateSliderLabels;
        UpdateSliderLabels(0);
        _camera.MakeCurrent();
        GenerateMap();
        ResetView();
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
        if (GodotObject.IsInstanceValid(_togglePanel)) _togglePanel.Pressed -= TogglePanel;
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
        if (GodotObject.IsInstanceValid(_inlets)) _inlets.ValueChanged -= UpdateSliderLabels;
        if (GodotObject.IsInstanceValid(_riverReach)) _riverReach.ValueChanged -= UpdateSliderLabels;
        if (GodotObject.IsInstanceValid(_riverWinding)) _riverWinding.ValueChanged -= UpdateSliderLabels;
        if (GodotObject.IsInstanceValid(_tributaries)) _tributaries.ValueChanged -= UpdateSliderLabels;
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
        // Only the right-hand drawing area counts when the settings panel is open.
        float left = _sidePanel.Visible
            ? _sidePanel.Position.X + _sidePanel.Size.X + 35f : 28f;
        float right = viewport.X - 32f;
        float availableWidth = Mathf.Max(220f, right - left);
        float availableHeight = Mathf.Max(220f, viewport.Y - 90f);

        // Frame the *actual island*, including islets, rather than empty ocean.
        Vector2 min = new(float.MaxValue, float.MaxValue);
        Vector2 max = new(float.MinValue, float.MinValue);
        Include(_art.Layout.Coast);
        foreach (Vector2[] island in _art.Layout.Islets) Include(island);
        void Include(Vector2[] points)
        {
            foreach (Vector2 point in points)
            {
                min = min.Min(point);
                max = max.Max(point);
            }
        }
        Vector2 span = max - min;
        float zoom = Mathf.Clamp(Mathf.Min(
            availableWidth / Mathf.Max(200f, span.X * 1.12f),
            availableHeight / Mathf.Max(180f, span.Y * 1.15f)), MinZoom, 2.5f);
        _camera.Zoom = new Vector2(zoom, zoom);
        Vector2 targetScreen = new(left + availableWidth * 0.5f, viewport.Y * 0.5f);
        _camera.Position = (min + max) * 0.5f +
            (viewport * 0.5f - targetScreen) / zoom;
    }

    // =========================================================
    private void TogglePanel()
    {
        _sidePanel.Visible = !_sidePanel.Visible;
        _togglePanel.Text = _sidePanel.Visible ? "Hide settings" : "Show settings";
        _togglePanel.Position = new Vector2(_sidePanel.Visible ? 365f : 16f, 17f);
        ResetView();
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
        _inlets.Editable = procedural;
        _riverReach.Editable = procedural;
        _riverWinding.Editable = procedural;
        _tributaries.Editable = procedural;
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
        _inletsText.Text = $"Major ocean inlets: {(int)_inlets.Value}";
        _reachText.Text = $"Inland channel reach: {(int)_riverReach.Value}%";
        _windingText.Text = $"Channel meandering: {(int)_riverWinding.Value}%";
        _tributariesText.Text = $"Maximum joining branches: {(int)_tributaries.Value}";
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
                (int)_ridgeLength.Value, (int)_inlets.Value,
                (int)_riverReach.Value, (int)_riverWinding.Value,
                (int)_tributaries.Value);
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
              $"other land components: {layout.Islets.Count} (offshore target {parameters.Value.OffshoreIslands})\n" +
              $"Mountains: {layout.MountainRidges.Count} (range " +
              $"{parameters.Value.MinMountainRegions}–{parameters.Value.MaxMountainRegions}) | " +
              $"Master: {MapConceptMasterElevation.Resolution}×{MapConceptMasterElevation.Resolution}" +
              $" / {MapConceptMasterElevation.WorldMetres / 1000f:0} km square\n" +
              $"Master step: {MapConceptMasterElevation.MetresPerSample:F2} m | " +
              $"Preview: {MapConceptElevationGrid.Columns}×{MapConceptElevationGrid.Rows}\n" +
              $"Ocean inlets: {layout.Waterways.FindAll(w => w.Kind == ConceptWaterKind.OceanInlet).Count} | " +
              $"Rivers: {layout.Waterways.FindAll(w => w.Kind == ConceptWaterKind.River).Count} | " +
              $"Channel branches: {layout.Waterways.FindAll(w => w.Kind == ConceptWaterKind.Tributary).Count}\n" +
              "Continuous sea-level corridors; isolated lakes are MAP2.2B."
            : $"Authored preset Island 1 | Decorative seed {seed}\n" +
              "Original forests, mountains, roads and sites preserved.";
        GD.Print($"[MAP2.2A] {(procedural ? "Procedural island" : "Authored preset")} seed {seed}; " +
            $"land {MapConceptGenerator.LandPercent(layout):F1}%; islets {layout.Islets.Count}.");
    }

    // =========================================================
    private void OnLabelsToggled(bool show) => _art.SetLabelsVisible(show);

    // =========================================================
    // Export the currently displayed map, not uncommitted seed-field text.
    private void ExportBlueprint()
    {
        const string path = "user://map_concept_layout.json";
        MapConceptMasterElevation? master = _art.Layout.MasterElevation;
        if (master != null)
        {
            string maskPath = "user://" + MapConceptMasterElevation.WaterFile;
            if (!master.SaveWaterMask(maskPath))
            {
                _status.Text = "Could not export 4096 water mask. See Godot Output.";
                GD.PushError("[MAP2.2A] Water mask export failed: " + maskPath);
                return;
            }
            string rawPath = "user://" + MapConceptMasterElevation.RawFile;
            if (!master.SaveRaw(rawPath))
            {
                _status.Text = "Could not export 4096 master elevation. See Godot Output.";
                GD.PushError("[MAP2.2A] Master R16 export failed: " + rawPath);
                return;
            }
        }

        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            _status.Text = "Could not export map blueprint JSON. See Godot Output.";
            GD.PushWarning("[MAP2.1B] Could not open JSON export.");
            return;
        }
        file.StoreString(_art.Layout.BlueprintJson(_activeSeed, _activeParameters));
        file.Flush();
        _status.Text = "Blueprint saved:\n" + ProjectSettings.GlobalizePath(path) +
            (master != null ? "\nMaster R16: " +
                ProjectSettings.GlobalizePath("user://" + MapConceptMasterElevation.RawFile) +
                "\nWater mask R8: " +
                ProjectSettings.GlobalizePath("user://" + MapConceptMasterElevation.WaterFile) : "");
        GD.Print("[MAP2.1B] Exported map: " + ProjectSettings.GlobalizePath(path));
    }
    #endregion
}
