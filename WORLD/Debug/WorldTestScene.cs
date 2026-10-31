// Standalone F1 world-test panel: numeric chunk counts and reusable terrain parameters.
using System;
using System.Collections.Generic;
using Godot;

public partial class WorldTestScene : Node3D
{
    private const string ControlsPath = "DebugUIRoot/WorldSettingsPanel/PanelScroll/SettingsContent";

    private WorldPreview _world = null!;
    private WorldScaleTree? _scaleTree;
    private Label _statistics = null!;
    private PanelContainer _settingsPanel = null!;
    private SpinBox _chunkCount = null!, _seed = null!;
    private OptionButton _detail = null!;
    private Button _regenerate = null!, _reset = null!;

    private readonly Dictionary<string, HSlider> _sliders = new();
    private readonly Dictionary<string, Label> _labels = new();

    private static readonly string[] SliderKeys =
        { "Meadow", "Valley", "Hills", "Mountain", "Peaks", "Sharpness", "Coverage" };

    #region Scene setup

    // =========================================================
    public override void _Ready()
    {
        _world = GetNode<WorldPreview>("World");
        _scaleTree = GetNodeOrNull<WorldScaleTree>("WorldActorsRoot/DebugPropsRoot/ScalePine_14m");
        _statistics = GetNode<Label>("DebugUIRoot/Statistics");
        _settingsPanel = GetNode<PanelContainer>("DebugUIRoot/WorldSettingsPanel");
        _chunkCount = GetNode<SpinBox>(ControlsPath + "/ChunkCount");
        _seed = GetNode<SpinBox>(ControlsPath + "/Seed");
        _detail = GetNode<OptionButton>(ControlsPath + "/DetailPreset");
        _regenerate = GetNode<Button>(ControlsPath + "/RegenerateButton");
        _reset = GetNode<Button>(ControlsPath + "/ResetButton");

        _detail.AddItem("2 m — detailed (step 1)", 1);
        _detail.AddItem("4 m — lighter (step 2)", 2);

        foreach (string key in SliderKeys)
        {
            HSlider slider = GetNode<HSlider>(ControlsPath + "/" + key + "Slider");
            _sliders.Add(key, slider);
            _labels.Add(key, GetNode<Label>(ControlsPath + "/" + key + "Label"));
            slider.ValueChanged += OnSliderValueChanged;
        }

        ResetControlsToWorldDefaults();
        _settingsPanel.Visible = false;
        _regenerate.Pressed += OnRegeneratePressed;
        _reset.Pressed += ResetControlsToWorldDefaults;
        _world.StatisticsChanged += UpdateStatistics;
        UpdateStatistics(_world.StatusText);
    }

    // =========================================================
    private void ResetControlsToWorldDefaults()
    {
        TerrainParameters settings = _world.Settings.Snapshot();
        _chunkCount.Value = _world.CurrentChunksPerSide;
        _seed.Value = settings.Seed;
        _detail.Select(_world.CurrentMeshStep == 2 ? 1 : 0);
        _sliders["Meadow"].Value = settings.BroadAmplitude;
        _sliders["Valley"].Value = settings.ValleyHalfWidth;
        _sliders["Hills"].Value = settings.RidgeAmplitude;
        _sliders["Mountain"].Value = settings.MountainAmplitude;
        _sliders["Peaks"].Value = settings.PeakAmplitude;
        _sliders["Sharpness"].Value = settings.PeakSharpness;
        _sliders["Coverage"].Value = settings.MountainCoverage * 100f;
        UpdateSliderLabels();
    }
    #endregion

    #region F1 input and generation

    // =========================================================
    public override void _Input(InputEvent input)
    {
        if (input is not InputEventKey { Pressed: true, Echo: false } key
            || key.PhysicalKeycode != Key.F1) return;

        _settingsPanel.Visible = !_settingsPanel.Visible;
        Input.MouseMode = _settingsPanel.Visible
            ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Captured;
        GetViewport().SetInputAsHandled();
    }

    // =========================================================
    private void OnRegeneratePressed()
    {
        // Commit text currently being typed, even if Enter hasn't been pressed.
        _chunkCount.Apply();
        _seed.Apply();

        int count = Math.Clamp((int)_chunkCount.Value, 1, 27);
        int step = _detail.GetSelectedId();
        TerrainParameters parameters = _world.Settings.Snapshot() with
        {
            Seed = (int)_seed.Value,
            BroadAmplitude = (float)_sliders["Meadow"].Value,
            ValleyHalfWidth = (float)_sliders["Valley"].Value,
            RidgeAmplitude = (float)_sliders["Hills"].Value,
            MountainAmplitude = (float)_sliders["Mountain"].Value,
            PeakAmplitude = (float)_sliders["Peaks"].Value,
            PeakSharpness = (float)_sliders["Sharpness"].Value,
            MountainCoverage = (float)_sliders["Coverage"].Value / 100f
        };

        _world.RegenerateWorld(count, step, parameters);
        if (_scaleTree != null && GodotObject.IsInstanceValid(_scaleTree))
            _scaleTree.SnapToTerrain(parameters);
    }

    // =========================================================
    private void OnSliderValueChanged(double value) => UpdateSliderLabels();

    // =========================================================
    private void UpdateSliderLabels()
    {
        _labels["Meadow"].Text = $"Meadow variation: {_sliders["Meadow"].Value:F1} m";
        _labels["Valley"].Text = $"Valley half-width: {_sliders["Valley"].Value:F0} m";
        _labels["Hills"].Text = $"Rolling hill height: {_sliders["Hills"].Value:F0} m";
        _labels["Mountain"].Text = $"Mountain mass: {_sliders["Mountain"].Value:F0} m";
        _labels["Peaks"].Text = $"Peak height: {_sliders["Peaks"].Value:F0} m";
        _labels["Sharpness"].Text = $"Peak sharpness: {_sliders["Sharpness"].Value:F2}";
        _labels["Coverage"].Text = $"Mountain coverage: {_sliders["Coverage"].Value:F0}%";
    }

    // =========================================================
    private void UpdateStatistics(string message) => _statistics.Text = message;

    // =========================================================
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_world)) _world.StatisticsChanged -= UpdateStatistics;
        if (GodotObject.IsInstanceValid(_regenerate)) _regenerate.Pressed -= OnRegeneratePressed;
        if (GodotObject.IsInstanceValid(_reset)) _reset.Pressed -= ResetControlsToWorldDefaults;
        foreach (HSlider slider in _sliders.Values)
            if (GodotObject.IsInstanceValid(slider)) slider.ValueChanged -= OnSliderValueChanged;
    }
    #endregion
}
