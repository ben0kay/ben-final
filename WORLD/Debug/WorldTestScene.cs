// Scene-only debug HUD adapter: subscribes once to the reusable World's build statistics.
using Godot;

public partial class WorldTestScene : Node3D
{
    private WorldPreview _world = null!;
    private Label _statistics = null!;

    // =========================================================
    public override void _Ready()
    {
        _world = GetNode<WorldPreview>("World");
        _statistics = GetNode<Label>("DebugHUD/Statistics");
        _world.StatisticsChanged += UpdateStatistics;
        UpdateStatistics(_world.StatusText);
    }

    // =========================================================
    private void UpdateStatistics(string message) => _statistics.Text = message;

    // =========================================================
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_world)) _world.StatisticsChanged -= UpdateStatistics;
    }
}
