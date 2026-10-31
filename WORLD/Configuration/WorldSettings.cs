// Godot-inspector knobs for the bounded static marching-cubes terrain preview.
using Godot;

[GlobalClass]
public partial class WorldSettings : Resource
{
    [ExportGroup("Preview Grid")]
    [Export(PropertyHint.Range, "0,13,1")] public int ChunkRadius { get; set; } = 13;
    [Export(PropertyHint.Range, "8,48,1")] public int CellsPerChunk { get; set; } = 32;
    [Export(PropertyHint.Range, "0.5,3,0.5")] public float CellSize { get; set; } = 2f;
    [Export] public bool GenerateCollision { get; set; } = true;
    // Manhattan/radial streaming is not implemented. This fixed square-grid proximity
    // filter determines which chunks receive physics: -1 none, 0 center, 1 center + neighbours.
    [Export(PropertyHint.Range, "-1,13,1")] public int CollisionChunkRadius { get; set; } = 0;
    // Keep the same metres per chunk. With default 32 nominal cells and 2 m cells,
    // MeshStep 1/2/4 yields 2 m / 4 m / 8 m surface sampling respectively.
    [Export(PropertyHint.Range, "1,4,1")] public int MeshStep { get; set; } = 1;

    [ExportGroup("Debug Performance")]
    [Export(PropertyHint.Range, "1,50,0.5")] public float MainThreadWarningMs { get; set; } = 8f;
    [Export(PropertyHint.Range, "16.7,100,0.1")] public float FrameSpikeThresholdMs { get; set; } = 33.3f;

    [ExportGroup("Geography - Lowlands")]
    [Export] public int Seed { get; set; } = 147;
    [Export] public float BaseHeight { get; set; } = 14f;
    [Export(PropertyHint.Range, "0,25,0.5")] public float BroadAmplitude { get; set; } = 8f;
    [Export(PropertyHint.Range, "0,12,0.5")] public float DetailAmplitude { get; set; } = 1.5f;
    [Export(PropertyHint.Range, "20,160,5")] public float ValleyHalfWidth { get; set; } = 90f;

    [ExportGroup("Geography - Foothills")]
    // Preserve the original export name for saved resources; this now controls hill elevation.
    [Export(PropertyHint.Range, "0,80,1")] public float RidgeAmplitude { get; set; } = 30f;

    [ExportGroup("Geography - Mountain Range")]
    [Export(PropertyHint.Range, "0,200,5")] public float MountainAmplitude { get; set; } = 95f;
    [Export(PropertyHint.Range, "0,150,5")] public float PeakAmplitude { get; set; } = 80f;
    [Export(PropertyHint.Range, "1.0,8.0,0.25")] public float PeakSharpness { get; set; } = 2.75f;
    [Export(PropertyHint.Range, "0,1,0.05")] public float MountainCoverage { get; set; } = 0.65f;
    // Negative Z lies ahead of the fly camera; mountains rise across the far side.
    [Export] public float MountainStartZ { get; set; } = -40f;
    [Export] public float MountainFullZ { get; set; } = -220f;

    [ExportGroup("Terrain Vertical Range")]
    // Only surface-crossing cells are evaluated; this range limits supported altitude.
    [Export] public float TerrainBottomY { get; set; } = -16f;
    [Export] public float TerrainTopY { get; set; } = 300f;

    // =========================================================
    // Snapshot values before the worker starts; no Godot Resource is read on that worker.
    public TerrainParameters Snapshot()
    {
        int cells = Mathf.Clamp(CellsPerChunk, 8, 48), radius = Mathf.Clamp(ChunkRadius, 0, 13);
        int requestedStep = MeshStep >= 4 ? 4 : MeshStep >= 2 ? 2 : 1;
        // Use only exact divisors so every chunk retains its original physical width.
        int step = cells % requestedStep == 0 ? requestedStep : 1;
        return new TerrainParameters(
            Seed, cells, Mathf.Clamp(CellSize, 0.5f, 3f), radius, 2 * radius + 1,
            GenerateCollision, Mathf.Clamp(CollisionChunkRadius, -1, radius), step,
            Mathf.Clamp(MainThreadWarningMs, 1f, 50f),
            Mathf.Clamp(FrameSpikeThresholdMs, 16.7f, 100f),
            BaseHeight, BroadAmplitude, DetailAmplitude, RidgeAmplitude,
            Mathf.Max(20f, ValleyHalfWidth), Mathf.Max(0f, MountainAmplitude),
            Mathf.Max(0f, PeakAmplitude), Mathf.Clamp(PeakSharpness, 1f, 8f),
            Mathf.Clamp(MountainCoverage, 0f, 1f), MountainStartZ, Mathf.Min(MountainFullZ, MountainStartZ - 10f),
            Mathf.Min(TerrainBottomY, TerrainTopY - 2f), Mathf.Max(TerrainTopY, TerrainBottomY + 2f));
    }
}

// Immutable managed settings passed to the pure terrain generator.
public readonly record struct TerrainParameters(
    int Seed, int CellsPerChunk, float CellSize, int ChunkRadius, int ChunksPerSide,
    bool GenerateCollision, int CollisionChunkRadius, int MeshStep,
    float MainThreadWarningMs, float FrameSpikeThresholdMs,
    float BaseHeight, float BroadAmplitude, float DetailAmplitude, float RidgeAmplitude,
    float ValleyHalfWidth, float MountainAmplitude, float PeakAmplitude, float PeakSharpness,
    float MountainCoverage, float MountainStartZ, float MountainFullZ, float TerrainBottomY, float TerrainTopY)
{
    public float ChunkWidth => CellsPerChunk * CellSize;
}
