// Finite 3x3 terrain preview: worker generates CPU arrays; Godot creates/render/collision nodes on the main thread.
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Godot;

public partial class WorldPreview : Node3D
{
    [Export] public WorldSettings Settings { get; set; } = null!;
    [Export] public Shader TerrainShader { get; set; } = null!;

    public event Action<string>? StatisticsChanged;
    public string StatusText { get; private set; } = "Preparing terrain generation...";

    private readonly ConcurrentQueue<TerrainChunkData> _completed = new();
    private CancellationTokenSource? _cancellation;
    private Task? _generator;
    private Node3D _terrainRoot = null!;
    private ShaderMaterial _material = null!;
    private int _total, _attached, _vertices, _triangles;
    private double _generationMilliseconds, _uploadMilliseconds;

    // =========================================================
    // Only immutable settings and managed data go into the worker thread.
    public override void _Ready()
    {
        _terrainRoot = GetNode<Node3D>("TerrainRoot");
        _material = new ShaderMaterial { Shader = TerrainShader };
        TerrainParameters config = Settings.Snapshot();
        _total = (config.ChunkRadius * 2 + 1) * (config.ChunkRadius * 2 + 1);
        _cancellation = new CancellationTokenSource();
        CancellationToken token = _cancellation.Token;
        _generator = Task.Run(() => GenerateTerrain(config, token), token);
        PublishStatus();
        SetProcess(true);
    }

    // =========================================================
    // The generator never touches Godot resources, the scene tree or render/physics servers.
    private void GenerateTerrain(TerrainParameters config, CancellationToken cancel)
    {
        var density = new DensityGenerator(config);
        for (int x = -config.ChunkRadius; x <= config.ChunkRadius; x++)
        for (int z = -config.ChunkRadius; z <= config.ChunkRadius; z++)
        {
            if (cancel.IsCancellationRequested) return;
            var stopwatch = Stopwatch.StartNew();
            TerrainChunkData result = MarchingCubesMesher.Build(config, density, x, z);
            stopwatch.Stop();
            result.GenerationMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
            if (!cancel.IsCancellationRequested) _completed.Enqueue(result);
        }
    }

    // =========================================================
    // At most one chunk mesh + collider is uploaded per frame; sleep after construction.
    public override void _Process(double delta)
    {
        if (_completed.TryDequeue(out TerrainChunkData? data))
        {
            var stopwatch = Stopwatch.StartNew();
            AttachTerrain(data);
            stopwatch.Stop();
            _generationMilliseconds += data.GenerationMilliseconds;
            _uploadMilliseconds += stopwatch.Elapsed.TotalMilliseconds;
            _attached++;
            _vertices += data.Vertices.Length;
            _triangles += data.Indices.Length / 3;
            PublishStatus();
        }

        if (_generator is { IsFaulted: true })
        {
            StatusText = "Terrain generation error: " + _generator.Exception?.GetBaseException().Message;
            StatisticsChanged?.Invoke(StatusText);
            GD.PushError(StatusText); SetProcess(false);
        }
        else if (_generator is { IsCompleted: true } && _completed.IsEmpty)
        {
            PublishStatus();
            GD.Print("WORLD PREVIEW FINISHED: " + StatusText.Replace("\n", " | "));
            SetProcess(false);
        }
    }

    // =========================================================
    // Only this main-thread step allocates ArrayMesh, MeshInstance and physics shapes.
    private void AttachTerrain(TerrainChunkData data)
    {
        if (data.Vertices.Length == 0) return;
        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = data.Vertices;
        arrays[(int)Mesh.ArrayType.Normal] = data.Normals;
        arrays[(int)Mesh.ArrayType.Index] = data.Indices;
        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);

        float width = Settings.CellsPerChunk * Settings.CellSize;
        var chunk = new Node3D
        {
            Name = "Chunk_" + data.ChunkX + "_" + data.ChunkZ,
            Position = new Vector3(data.ChunkX * width, 0f, data.ChunkZ * width)
        };
        _terrainRoot.AddChild(chunk);
        chunk.AddChild(new MeshInstance3D { Name = "Surface", Mesh = mesh, MaterialOverride = _material });

        if (!Settings.GenerateCollision) return;
        var staticBody = new StaticBody3D { Name = "TerrainCollision" };
        chunk.AddChild(staticBody);
        staticBody.AddChild(new CollisionShape3D { Name = "Shape", Shape = mesh.CreateTrimeshShape() });
    }

    // =========================================================
    private void PublishStatus()
    {
        StatusText = "Chunks: " + _attached + " / " + _total
            + "\nVertices: " + _vertices + "   Triangles: " + _triangles
            + "\nCPU meshing: " + _generationMilliseconds.ToString("F1") + " ms total"
            + "\nMain-thread mesh/collision uploads: " + _uploadMilliseconds.ToString("F1") + " ms total";
        StatisticsChanged?.Invoke(StatusText);
    }

    // =========================================================
    public override void _ExitTree()
    {
        _cancellation?.Cancel();
        // No wait on the game thread. Pure CPU worker exits at the next chunk boundary.
    }
}
