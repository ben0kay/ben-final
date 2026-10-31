// Godot-inspector knobs for the bounded static marching-cubes terrain preview.
using Godot;

[GlobalClass]
public partial class WorldSettings : Resource
{
    [ExportGroup("Preview Grid")]
    [Export(PropertyHint.Range, "0,2,1")] public int ChunkRadius { get; set; } = 1;
    [Export(PropertyHint.Range, "8,48,1")] public int CellsPerChunk { get; set; } = 32;
    [Export(PropertyHint.Range, "0.5,3,0.5")] public float CellSize { get; set; } = 1f;
    [Export] public bool GenerateCollision { get; set; } = true;

    [ExportGroup("Geography")]
    [Export] public int Seed { get; set; } = 147;
    [Export] public float BaseHeight { get; set; } = 14f;
    [Export] public float BroadAmplitude { get; set; } = 7f;
    [Export] public float DetailAmplitude { get; set; } = 2f;
    [Export] public float RidgeAmplitude { get; set; } = 6f;

    // =========================================================
    // Snapshot values before the worker starts; no Godot Resource is read on that worker.
    public TerrainParameters Snapshot() => new(
        Seed, Mathf.Clamp(CellsPerChunk, 8, 48), Mathf.Clamp(CellSize, 0.5f, 3f),
        Mathf.Clamp(ChunkRadius, 0, 2), GenerateCollision,
        BaseHeight, BroadAmplitude, DetailAmplitude, RidgeAmplitude);
}

// Immutable managed settings passed to the pure terrain generator.
public readonly record struct TerrainParameters(
    int Seed, int CellsPerChunk, float CellSize, int ChunkRadius,
    bool GenerateCollision, float BaseHeight, float BroadAmplitude,
    float DetailAmplitude, float RidgeAmplitude)
{
    public float ChunkWidth => CellsPerChunk * CellSize;
}
