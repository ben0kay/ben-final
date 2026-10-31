// Debug-only F1 settings: choose a finite terrain preset and rebuild without editing WorldMaster.tres.
using Godot;

public partial class WorldTestScene : Node3D
{
    private WorldPreview _world = null!;
    private Label _statistics = null!;
    private PanelContainer _settingsPanel = null!;
    private OptionButton _sizeOption = null!;
    private OptionButton _detailOption = null!;
    private Button _regenerateButton = null!;

    #region Scene and controls

    // =========================================================
    public override void _Ready()
    {
        _world = GetNode<WorldPreview>("World");
        _statistics = GetNode<Label>("DebugUIRoot/Statistics");
        _settingsPanel = GetNode<PanelContainer>("DebugUIRoot/WorldSettingsPanel");
        _sizeOption = GetNode<OptionButton>("DebugUIRoot/WorldSettingsPanel/SettingsContent/SizePreset");
        _detailOption = GetNode<OptionButton>("DebugUIRoot/WorldSettingsPanel/SettingsContent/DetailPreset");
        _regenerateButton = GetNode<Button>("DebugUIRoot/WorldSettingsPanel/SettingsContent/RegenerateButton");

        // IDs are chunk radii; 2*radius+1 gives 9 / 17 / 27 chunks per side.
        _sizeOption.AddItem("576 m — 9 x 9 chunks", 4);
        _sizeOption.AddItem("1,088 m — 17 x 17 chunks", 8);
        _sizeOption.AddItem("1,728 m — 27 x 27 chunks (9x area)", 13);
        _sizeOption.Select(_world.CurrentChunkRadius == 4 ? 0 :
            _world.CurrentChunkRadius == 8 ? 1 : 2);

        _detailOption.AddItem("2 m — detailed (MeshStep 1)", 1);
        _detailOption.AddItem("4 m — lighter (MeshStep 2)", 2);
        _detailOption.Select(_world.CurrentMeshStep == 2 ? 1 : 0);

        _settingsPanel.Visible = false;
        _regenerateButton.Pressed += OnRegeneratePressed;
        _world.StatisticsChanged += UpdateStatistics;
        UpdateStatistics(_world.StatusText);
    }

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
        _world.RegenerateWorld(_sizeOption.GetSelectedId(), _detailOption.GetSelectedId());
    }

    // =========================================================
    private void UpdateStatistics(string message) => _statistics.Text = message;

    // =========================================================
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_world)) _world.StatisticsChanged -= UpdateStatistics;
        if (GodotObject.IsInstanceValid(_regenerateButton))
            _regenerateButton.Pressed -= OnRegeneratePressed;
    }
    #endregion
}
