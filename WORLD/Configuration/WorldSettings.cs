// Godot-inspector knobs for the bounded static marching-cubes terrain preview.
using Godot;

[GlobalClass]
public partial class WorldSettings : Resource
{
    [ExportGroup("Preview Grid")]
    [Export(PropertyHint.Range, "0,5,1")] public int ChunkRadius { get; set; } = 4;
    [Export(PropertyHint.Range, "8,48,1")] public int CellsPerChunk { get; set; } = 32;
    [Export(PropertyHint.Range, "0.5,3,0.5")] public float CellSize { get; set; } = 2f;
    [Export] public bool GenerateCollision { get; set; } = true;
    // Manhattan/radial streaming is not implemented. This fixed square-grid proximity
    // filter determines which chunks receive physics: -1 none, 0 center, 1 center + neighbours.
    [Export(PropertyHint.Range, "-1,5,1")] public int CollisionChunkRadius { get; set; } = 0;
    // Keep the same metres per chunk. With default 32 nominal cells and 2 m cells,
    // MeshStep 1/2/4 yields 2 m / 4 m / 8 m surface sampling respectively.
    [Export(PropertyHint.Range, "1,4,1")] public int MeshStep { get; set; } = 1;

    [ExportGroup("Debug Performance")]
    [Export(PropertyHint.Range, "1,50,0.5")] public float MainThreadWarningMs { get; set; } = 8f;
    [Export(PropertyHint.Range, "16.7,100,0.1")] public float FrameSpikeThresholdMs { get; set; } = 33.3f;

    [ExportGroup("Geography")]
    [Export] public int Seed { get; set; } = 147;
    [Export] public float BaseHeight { get; set; } = 14f;
    [Export] public float BroadAmplitude { get; set; } = 7f;
    [Export] public float DetailAmplitude { get; set; } = 2f;
    [Export] public float RidgeAmplitude { get; set; } = 6f;

    // =========================================================
    // Snapshot values before the worker starts; no Godot Resource is read on that worker.
    public TerrainParameters Snapshot()
    {
        int cells = Mathf.Clamp(CellsPerChunk, 8, 48), radius = Mathf.Clamp(ChunkRadius, 0, 5);
        int requestedStep = MeshStep >= 4 ? 4 : MeshStep >= 2 ? 2 : 1;
        // Use only exact divisors so every chunk retains its original physical width.
        int step = cells % requestedStep == 0 ? requestedStep : 1;
        return new TerrainParameters(
            Seed, cells, Mathf.Clamp(CellSize, 0.5f, 3f), radius,
            GenerateCollision, Mathf.Clamp(CollisionChunkRadius, -1, radius), step,
            Mathf.Clamp(MainThreadWarningMs, 1f, 50f),
            Mathf.Clamp(FrameSpikeThresholdMs, 16.7f, 100f),
            BaseHeight, BroadAmplitude, DetailAmplitude, RidgeAmplitude);
    }
}

// Immutable managed settings passed to the pure terrain generator.
public readonly record struct TerrainParameters(
    int Seed, int CellsPerChunk, float CellSize, int ChunkRadius,
    bool GenerateCollision, int CollisionChunkRadius, int MeshStep,
    float MainThreadWarningMs, float FrameSpikeThresholdMs,
    float BaseHeight, float BroadAmplitude, float DetailAmplitude, float RidgeAmplitude)
{
    public float ChunkWidth => CellsPerChunk * CellSize;
}
