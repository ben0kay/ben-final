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
    private LineEdit _chunkCount = null!, _seed = null!;
    private ConfirmationDialog _largeWorldConfirmation = null!;
    private int _pendingCount, _pendingStep;
    private TerrainParameters _pendingParameters;
    private Label _sizeWarning = null!;
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
        _chunkCount = GetNode<LineEdit>(ControlsPath + "/ChunkCount");
        _sizeWarning = GetNode<Label>(ControlsPath + "/SizeWarning");
        _largeWorldConfirmation = GetNode<ConfirmationDialog>("DebugRoot/LargeWorldConfirmation");
        _seed = GetNode<LineEdit>(ControlsPath + "/Seed");
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

        _chunkCount.TextChanged += OnChunkCountChanged;
        _largeWorldConfirmation.Confirmed += OnLargeWorldConfirmed;
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
        _chunkCount.Text = _world.CurrentChunksPerSide.ToString();
        _seed.Text = settings.Seed.ToString();
        _detail.Select(_world.CurrentMeshStep == 2 ? 1 : 0);
        _sliders["Meadow"].Value = settings.BroadAmplitude;
        _sliders["Valley"].Value = settings.ValleyHalfWidth;
        _sliders["Hills"].Value = settings.RidgeAmplitude;
        _sliders["Mountain"].Value = settings.MountainAmplitude;
        _sliders["Peaks"].Value = settings.PeakAmplitude;
        _sliders["Sharpness"].Value = settings.PeakSharpness;
        _sliders["Coverage"].Value = settings.MountainCoverage * 100f;
        UpdateSliderLabels();
        UpdateWorldSizeHint();
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
        // Read actual typed text, NOT a previously committed SpinBox numeric value.
        if (!TryReadChunkCount(out int count))
        {
            _sizeWarning.Text = "Enter a whole-number grid size between 1 and 100.";
            return;
        }
        if (!int.TryParse(_seed.Text.Trim(), out int seed) || seed < 0 || seed > 99999)
        {
            _sizeWarning.Text = "Invalid seed. Enter a number from 0 to 99999.";
            return;
        }

        int step = _detail.GetSelectedId();
        TerrainParameters parameters = _world.Settings.Snapshot() with
        {
            Seed = seed,
            BroadAmplitude = (float)_sliders["Meadow"].Value,
            ValleyHalfWidth = (float)_sliders["Valley"].Value,
            RidgeAmplitude = (float)_sliders["Hills"].Value,
            MountainAmplitude = (float)_sliders["Mountain"].Value,
            PeakAmplitude = (float)_sliders["Peaks"].Value,
            PeakSharpness = (float)_sliders["Sharpness"].Value,
            MountainCoverage = (float)_sliders["Coverage"].Value / 100f
        };

        if (count > 50)
        {
            _pendingCount = count;
            _pendingStep = step;
            _pendingParameters = parameters;
            _largeWorldConfirmation.DialogText =
                $"Generate {count} x {count} = {count * count:N0} static chunks?\n\n" +
                "All meshes remain resident. A very large world may exhaust RAM/VRAM " +
                "or freeze Godot while loading. This is a stress test, not streaming.\n\n" +
                "Use 4 m detail if the 2 m setting is too expensive.";
            _largeWorldConfirmation.PopupCentered();
            return;
        }

        StartRegeneration(count, step, parameters);
    }

    // =========================================================
    // Never interpret invalid input as the previous size (27) or silently clamp.
    private bool TryReadChunkCount(out int count) =>
        int.TryParse(_chunkCount.Text.Trim(), out count) && count is >= 1 and <= 100;

    // =========================================================
    private void OnLargeWorldConfirmed() =>
        StartRegeneration(_pendingCount, _pendingStep, _pendingParameters);

    // =========================================================
    private void StartRegeneration(int count, int step, TerrainParameters parameters)
    {
        _sizeWarning.Text = $"Requested: {count} x {count} = {count * count:N0} chunks — generating...";
        GD.Print($"[WORLD F1] User requested {count} x {count} chunks, mesh step {step}, seed {parameters.Seed}.");
        _world.RegenerateWorld(count, step, parameters);
        if (_scaleTree != null && GodotObject.IsInstanceValid(_scaleTree))
            _scaleTree.SnapToTerrain(parameters);
    }

    // =========================================================
    // Update a lightweight estimate without regenerating or allocating any terrain.
    private void OnChunkCountChanged(string text) => UpdateWorldSizeHint();

    // =========================================================
    private void UpdateWorldSizeHint()
    {
        if (!TryReadChunkCount(out int count))
        {
            _sizeWarning.Text = "Enter a whole-number grid size between 1 and 100.";
            return;
        }
        int chunks = count * count;
        float metres = count * _world.Settings.Snapshot().ChunkWidth;
        string warning = count > 50
            ? "\nEXTREME load: confirmation required. No chunk streaming."
            : count >= 30 ? "\nHigh load: all chunks stay resident. Try 4 m detail." : "";
        _sizeWarning.Text = $"{count} x {count} = {chunks:N0} chunks | {metres:N0} m across" + warning;
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
        if (GodotObject.IsInstanceValid(_chunkCount)) _chunkCount.TextChanged -= OnChunkCountChanged;
        if (GodotObject.IsInstanceValid(_largeWorldConfirmation))
            _largeWorldConfirmation.Confirmed -= OnLargeWorldConfirmed;
        if (GodotObject.IsInstanceValid(_regenerate)) _regenerate.Pressed -= OnRegeneratePressed;
        if (GodotObject.IsInstanceValid(_reset)) _reset.Pressed -= ResetControlsToWorldDefaults;
        foreach (HSlider slider in _sliders.Values)
            if (GodotObject.IsInstanceValid(slider)) slider.ValueChanged -= OnSliderValueChanged;
    }
    #endregion
}
