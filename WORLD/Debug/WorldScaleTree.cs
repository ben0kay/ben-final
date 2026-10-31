// Test-only tree landmark: position a known-height mesh on the generated terrain surface.
using Godot;

public partial class WorldScaleTree : Node3D
{
    [Export] public NodePath WorldNodePath { get; set; } = new NodePath("../World");
    [Export(PropertyHint.Range, "0,1,0.05")] public float GroundSink { get; set; } = 0.3f;

    // =========================================================
    // Place the trunk bottom on the same deterministic terrain used by the preview.
    // Keep this as a single initialization, never an idle per-frame terrain query.
    public override void _Ready()
    {
        WorldPreview? world = GetNodeOrNull<WorldPreview>(WorldNodePath);
        if (world?.Settings == null)
        {
            GD.PushWarning("[WORLD] Scale tree could not find the World preview settings.");
            return;
        }

        Vector3 point = GlobalPosition;
        var terrain = new DensityGenerator(world.Settings.Snapshot());
        float groundHeight = terrain.Height(point.X, point.Z);
        GlobalPosition = new Vector3(point.X, groundHeight - GroundSink, point.Z);
    }
}
