// Standalone F1 world-test panel: numeric chunk counts and reusable terrain parameters.
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    private WorldFlyCamera _flyCamera = null!;
    private Label _benchmarkHud = null!, _benchmarkDetails = null!;
    private OptionButton _benchmarkSpeed = null!;
    private CheckBox _benchmarkTeleport = null!;
    private Button _benchmarkStart = null!, _benchmarkRoute = null!, _benchmarkStop = null!;
    private readonly FrameTimeRecorder _frames = new();
    private readonly List<string> _spikeExamples = new(8);
    private readonly Stopwatch _captureClock = new();
    private string _captureName = "No capture yet";
    private double _displayRefresh, _lastFrameMs;
    private double _spikeThreshold = 33.3;
    private long _peakManagedBytes;
    private int _initialGc0, _initialGc1, _initialGc2;

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
        _flyCamera = GetNode<WorldFlyCamera>("DebugRoot/DebugCamera");
        _benchmarkHud = GetNode<Label>("DebugUIRoot/BenchmarkHud");
        _benchmarkDetails = GetNode<Label>(ControlsPath + "/BenchmarkDetails");
        _benchmarkSpeed = GetNode<OptionButton>(ControlsPath + "/BenchmarkSpeed");
        _benchmarkTeleport = GetNode<CheckBox>(ControlsPath + "/BenchmarkTeleport");
        _benchmarkStart = GetNode<Button>(ControlsPath + "/BenchmarkStartButton");
        _benchmarkRoute = GetNode<Button>(ControlsPath + "/BenchmarkRouteButton");
        _benchmarkStop = GetNode<Button>(ControlsPath + "/BenchmarkStopButton");
        _benchmarkSpeed.AddItem("Normal flight (60 m/s)", 60);
        _benchmarkSpeed.AddItem("Fast flight (180 m/s)", 180);
        _benchmarkSpeed.Select(0);
        _spikeThreshold = _world.Settings.Snapshot().FrameSpikeThresholdMs;
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
        _benchmarkStart.Pressed += OnStartManualCapture;
        _benchmarkRoute.Pressed += OnStartRoute;
        _benchmarkStop.Pressed += OnStopCapture;
        _flyCamera.BenchmarkRouteFinished += OnRouteFinished;
        UpdateStatistics(_world.StatusText);
        RefreshBenchmarkDisplay();
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

    #region Benchmark instrumentation (BW-W4.0)

    // =========================================================
    // The world generator stops processing after loading. This light, constant-work
    // sampler remains active so a capture can include traversal and idle frames.
    public override void _Process(double delta)
    {
        if (!GetWindow().HasFocus()) return;
        _lastFrameMs = delta * 1000.0;
        if (_frames.IsRecording)
        {
            _frames.Sample(_lastFrameMs, _spikeThreshold);
            // Only allocate for the first few slow frames; no per-frame log spam.
            if (_lastFrameMs >= _spikeThreshold && _spikeExamples.Count < 8)
                _spikeExamples.Add($"frame {_frames.FrameCount}: {_lastFrameMs:F1} ms, prior terrain {_world.PreviousFrameWork}");
        }
        _displayRefresh += delta;
        if (_displayRefresh < 0.5) return;
        _displayRefresh = 0;
        RefreshBenchmarkDisplay();
    }

    // =========================================================
    private void OnStartManualCapture()
    {
        _flyCamera.StopBenchmarkRoute();
        StartCapture("Manual flight");
    }

    // =========================================================
    private void OnStartRoute()
    {
        int speed = _benchmarkSpeed.GetSelectedId() == 180 ? 180 : 60;
        bool teleport = _benchmarkTeleport.ButtonPressed;
        StartCapture(teleport ? $"Route {speed} m/s + jump" : $"Route {speed} m/s");
        float width = _world.CurrentChunksPerSide * _world.Settings.Snapshot().ChunkWidth;
        _flyCamera.StartBenchmarkRoute(width, speed, teleport);
    }

    // =========================================================
    private void StartCapture(string name)
    {
        _flyCamera.StopBenchmarkRoute();
        _captureName = name;
        _frames.Start();
        _spikeExamples.Clear();
        _captureClock.Restart();
        _peakManagedBytes = GC.GetTotalMemory(false);
        _initialGc0 = GC.CollectionCount(0);
        _initialGc1 = GC.CollectionCount(1);
        _initialGc2 = GC.CollectionCount(2);
        // Closing F1 means manual flight and the scripted route have comparable UI load.
        _settingsPanel.Visible = false;
        Input.MouseMode = Input.MouseModeEnum.Captured;
        _displayRefresh = 0.5;
        RefreshBenchmarkDisplay();
        GD.Print($"[BW-W4.0] Started {_captureName}; world {_world.CurrentChunksPerSide}x{_world.CurrentChunksPerSide}, step {_world.CurrentMeshStep}.");
    }

    // =========================================================
    private void OnStopCapture()
    {
        _flyCamera.StopBenchmarkRoute();
        FinishCapture("Stopped");
    }

    // =========================================================
    private void OnRouteFinished() => FinishCapture("Route complete");

    // =========================================================
    private void FinishCapture(string reason)
    {
        if (!_frames.IsRecording) return;
        _frames.Stop();
        _captureClock.Stop();
        _peakManagedBytes = Math.Max(_peakManagedBytes, GC.GetTotalMemory(false));
        _captureName += $" — {reason}";
        RefreshBenchmarkDisplay();
        GD.Print("[BW-W4.0] " + _benchmarkDetails.Text.Replace("\n", " | "));
    }

    // =========================================================
    // Refresh at 2 Hz; never compute percentiles or query renderer/GC every frame.
    private void RefreshBenchmarkDisplay()
    {
        FrameTimeSnapshot s = _frames.Snapshot();
        long primitives = (long)Godot.Performance.GetMonitor(
            Godot.Performance.Monitor.RenderTotalPrimitivesInFrame);
        if (_frames.IsRecording)
            _peakManagedBytes = Math.Max(_peakManagedBytes, GC.GetTotalMemory(false));
        string mode = _frames.IsRecording ? "REC" : s.Frames > 0 ? "CAPTURED" : "IDLE";
        _benchmarkHud.Text =
            $"FPS {Engine.GetFramesPerSecond():F0} | frame delta {_lastFrameMs:F1} ms | " +
            $"P99 {(s.Frames > 0 ? $"{s.P99Ms:F1} ms" : "--")} | " +
            $"{mode} | rendered prims {primitives:N0}";

        if (s.Frames == 0)
        {
            _benchmarkDetails.Text = "No benchmark samples. Start a manual capture or scripted route.\n" +
                "Frame deltas are measured in the foreground; this is still a STATIC world.";
            return;
        }

        _benchmarkDetails.Text =
            $"{_captureName} | {mode} | {_captureClock.Elapsed.TotalSeconds:F1} s / {s.Frames:N0} frames\n" +
            $"Frame delta: avg {s.AverageMs:F1} | median {s.MedianMs:F1} | " +
            $"P95 {s.P95Ms:F1} | P99 {s.P99Ms:F1} | worst {s.WorstMs:F1} ms\n" +
            $">= {_spikeThreshold:F1} ms: {s.Spikes:N0} frames | previous terrain work: {_world.PreviousFrameWork}\n" +
            $"Managed heap peak sampled: {_peakManagedBytes / 1048576d:F1} MiB (not RAM/VRAM) | " +
            $"GC 0/1/2: {GC.CollectionCount(0) - _initialGc0}/" +
            $"{GC.CollectionCount(1) - _initialGc1}/{GC.CollectionCount(2) - _initialGc2}\n" +
            _world.BenchmarkWorkSummary +
            (_spikeExamples.Count > 0 ? "\nFirst slow frames (correlation only):\n" +
                string.Join("\n", _spikeExamples) : "");
    }
    #endregion

    // =========================================================
    private void UpdateStatistics(string message) => _statistics.Text = message;

    // =========================================================
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_world)) _world.StatisticsChanged -= UpdateStatistics;
        if (GodotObject.IsInstanceValid(_flyCamera))
        {
            _flyCamera.BenchmarkRouteFinished -= OnRouteFinished;
            _flyCamera.StopBenchmarkRoute();
        }
        if (GodotObject.IsInstanceValid(_benchmarkStart)) _benchmarkStart.Pressed -= OnStartManualCapture;
        if (GodotObject.IsInstanceValid(_benchmarkRoute)) _benchmarkRoute.Pressed -= OnStartRoute;
        if (GodotObject.IsInstanceValid(_benchmarkStop)) _benchmarkStop.Pressed -= OnStopCapture;
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
