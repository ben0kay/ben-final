// Bounded 3x3 preview: one pure managed mesher worker; separate main-thread mesh and physics work.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
    private readonly Queue<PendingCollision> _pendingCollisions = new();
    private CancellationTokenSource? _cancellation;
    private Task? _generator;
    private Node3D _terrainRoot = null!;
    private ShaderMaterial _material = null!;
    private TerrainParameters _parameters;
    private int _total, _attached, _colliders, _vertices, _triangles, _frameSpikes;
    private double _cpuTotalMs, _cpuMaxMs, _meshTotalMs, _meshMaxMs;
    private double _collisionTotalMs, _collisionMaxMs, _maxObservedFrameMs;

    // Collision is deferred until a different frame from its mesh creation.
    private readonly record struct PendingCollision(ArrayMesh Mesh, Node3D Chunk, int X, int Z);

    #region Preview lifecycle

    // =========================================================
    // Snapshot exported resources on the main thread; never read them from the worker.
    public override void _Ready()
    {
        if (Settings == null || TerrainShader == null)
        {
            GD.PushError("[WORLD] Missing world settings or terrain shader.");
            SetProcess(false); return;
        }

        _terrainRoot = GetNode<Node3D>("TerrainRoot");
        _material = new ShaderMaterial { Shader = TerrainShader };
        _parameters = Settings.Snapshot();
        _total = (2 * _parameters.ChunkRadius + 1) * (2 * _parameters.ChunkRadius + 1);
        _cancellation = new CancellationTokenSource();
        CancellationToken token = _cancellation.Token;
        _generator = Task.Run(() => GenerateTerrain(_parameters, token), token);
        PublishStatus();
        SetProcess(true);
    }

    // =========================================================
    // Pure CPU work is generated serially and bounded to the preview's chosen chunk radius.
    private void GenerateTerrain(TerrainParameters config, CancellationToken cancel)
    {
        var density = new DensityGenerator(config);
        for (int x = -config.ChunkRadius; x <= config.ChunkRadius; x++)
        for (int z = -config.ChunkRadius; z <= config.ChunkRadius; z++)
        {
            if (cancel.IsCancellationRequested) return;
            var timer = Stopwatch.StartNew();
            TerrainChunkData data = MarchingCubesMesher.Build(config, density, x, z);
            timer.Stop();
            data.GenerationMilliseconds = timer.Elapsed.TotalMilliseconds;
            if (!cancel.IsCancellationRequested) _completed.Enqueue(data);
        }
    }

    // =========================================================
    // At most ONE expensive operation each frame: mesh creation OR collider cooking.
    // Prioritize presenting geometry; collision work follows after queued meshes.
    public override void _Process(double delta)
    {
        bool diagnosticsChanged = false;
        double frameMilliseconds = delta * 1000.0;
        _maxObservedFrameMs = Math.Max(_maxObservedFrameMs, frameMilliseconds);
        if (frameMilliseconds >= _parameters.FrameSpikeThresholdMs)
        {
            _frameSpikes++;
            diagnosticsChanged = true;
            if (_frameSpikes <= 5)
                GD.Print($"[WORLD PERF] Frame delta spike: {frameMilliseconds:F2} ms " +
                    $"(threshold {_parameters.FrameSpikeThresholdMs:F1} ms, " +
                    $"meshes {_attached}/{_total}, colliders {_colliders}).");
        }

        if (_generator is { IsFaulted: true })
        {
            GD.PushError("[WORLD] Terrain generation failed: " + _generator.Exception?.GetBaseException().Message);
            StatusText = "Terrain generation failed. See Godot Output.";
            StatisticsChanged?.Invoke(StatusText);
            SetProcess(false);
            return;
        }

        try
        {
            if (_completed.TryDequeue(out TerrainChunkData? data))
            { InstallMesh(data); diagnosticsChanged = true; }
            else if (_pendingCollisions.Count > 0)
            { InstallCollision(_pendingCollisions.Dequeue()); diagnosticsChanged = true; }
        }
        catch (Exception error)
        {
            GD.PushError("[WORLD] Terrain setup failed: " + error);
            StatusText = "Terrain setup failed. See Godot Output.";
            StatisticsChanged?.Invoke(StatusText);
            _cancellation?.Cancel(); SetProcess(false); return;
        }

        if (diagnosticsChanged) PublishStatus();

        if (_generator is { IsCompleted: true } && _completed.IsEmpty && _pendingCollisions.Count == 0)
        {
            PublishStatus(); // Capture the last observed delta before stopping forever.
            GD.Print("[WORLD PERF] FINAL: " + StatusText.Replace("\n", " | "));
            SetProcess(false);
        }
    }

    // =========================================================
    public override void _ExitTree() => _cancellation?.Cancel();
    #endregion

    #region Mesh and collision operations

    // =========================================================
    // Timing covers mesh arrays, ArrayMesh surface creation and node setup.
    // This is MAIN-THREAD SETUP TIME, not guaranteed GPU completion time.
    private void InstallMesh(TerrainChunkData data)
    {
        var timer = Stopwatch.StartNew();
        bool addCollision = _parameters.GenerateCollision && _parameters.CollisionChunkRadius >= 0
            && Math.Max(Math.Abs(data.ChunkX), Math.Abs(data.ChunkZ)) <= _parameters.CollisionChunkRadius;

        if (data.Vertices.Length > 0)
        {
            var arrays = new Godot.Collections.Array();
            arrays.Resize((int)Mesh.ArrayType.Max);
            arrays[(int)Mesh.ArrayType.Vertex] = data.Vertices;
            arrays[(int)Mesh.ArrayType.Normal] = data.Normals;
            arrays[(int)Mesh.ArrayType.Index] = data.Indices;
            var mesh = new ArrayMesh();
            mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);

            float width = _parameters.ChunkWidth;
            var chunk = new Node3D
            {
                Name = $"Chunk_{data.ChunkX}_{data.ChunkZ}",
                Position = new Vector3(data.ChunkX * width, 0f, data.ChunkZ * width)
            };
            _terrainRoot.AddChild(chunk);
            chunk.AddChild(new MeshInstance3D { Name = "Surface", Mesh = mesh, MaterialOverride = _material });

            if (addCollision) _pendingCollisions.Enqueue(new PendingCollision(mesh, chunk, data.ChunkX, data.ChunkZ));
        }
        else addCollision = false;

        timer.Stop();
        double meshMs = timer.Elapsed.TotalMilliseconds;
        _attached++; _vertices += data.Vertices.Length; _triangles += data.Indices.Length / 3;
        _cpuTotalMs += data.GenerationMilliseconds;
        _cpuMaxMs = Math.Max(_cpuMaxMs, data.GenerationMilliseconds);
        _meshTotalMs += meshMs;
        _meshMaxMs = Math.Max(_meshMaxMs, meshMs);

        GD.Print($"[WORLD PERF] Chunk ({data.ChunkX},{data.ChunkZ}) step {_parameters.MeshStep}: " +
            $"CPU {data.GenerationMilliseconds:F2} ms | mesh setup {meshMs:F2} ms | " +
            $"{data.Indices.Length / 3} tris | collision {(addCollision ? "queued" : "skipped")}");
        WarnIfSlow("mesh setup", meshMs, data.ChunkX, data.ChunkZ);
    }

    // =========================================================
    // Trimesh creation is independently timed, on a DIFFERENT frame from mesh setup.
    private void InstallCollision(PendingCollision pending)
    {
        var timer = Stopwatch.StartNew();
        var staticBody = new StaticBody3D { Name = "TerrainCollision" };
        pending.Chunk.AddChild(staticBody);
        staticBody.AddChild(new CollisionShape3D { Name = "Shape", Shape = pending.Mesh.CreateTrimeshShape() });
        timer.Stop();

        double collisionMs = timer.Elapsed.TotalMilliseconds;
        _colliders++;
        _collisionTotalMs += collisionMs;
        _collisionMaxMs = Math.Max(_collisionMaxMs, collisionMs);
        GD.Print($"[WORLD PERF] Chunk ({pending.X},{pending.Z}): collision cook/setup {collisionMs:F2} ms.");
        WarnIfSlow("collision cooking", collisionMs, pending.X, pending.Z);
    }

    // =========================================================
    // Only warn when a measured individual operation exceeds the chosen debug threshold.
    private void WarnIfSlow(string operation, double milliseconds, int x, int z)
    {
        if (milliseconds < _parameters.MainThreadWarningMs) return;
        GD.Print($"[WORLD PERF] SLOW {operation} ({x},{z}): {milliseconds:F2} ms " +
            $"(warning >= {_parameters.MainThreadWarningMs:F1} ms).");
    }
    #endregion

    #region Diagnostics

    // =========================================================
    // HUD is event-driven from changes and sleeps after the static test is complete.
    private void PublishStatus()
    {
        string collisionMode = !_parameters.GenerateCollision || _parameters.CollisionChunkRadius < 0
            ? "none" : $"radius {_parameters.CollisionChunkRadius}";
        StatusText =
            $"Chunks: {_attached}/{_total} | Colliders: {_colliders} ({collisionMode}) | mesh step {_parameters.MeshStep}\n" +
            $"Vertices: {_vertices}   Triangles: {_triangles}\n" +
            $"CPU meshing: {_cpuTotalMs:F1} ms (max chunk {_cpuMaxMs:F1})\n" +
            $"Mesh setup: {_meshTotalMs:F1} ms (max chunk {_meshMaxMs:F1})\n" +
            $"Collision cook: {_collisionTotalMs:F1} ms (max chunk {_collisionMaxMs:F1})\n" +
            $"Max observed frame delta: {_maxObservedFrameMs:F1} ms | " +
            $">={_parameters.FrameSpikeThresholdMs:F1} ms spikes: {_frameSpikes}";
        StatisticsChanged?.Invoke(StatusText);
    }
    #endregion
}
