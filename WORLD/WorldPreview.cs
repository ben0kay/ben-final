// Finite procedural terrain preview: a CPU meshing worker, main-thread mesh/physics installation and profiling.
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
    private int _spikesAfterMesh, _spikesAfterCollision, _spikesOther, _postLoadFrames;
    // A frame's delta reflects elapsed time since the preceding frame, not this frame's new work.
    // Tagging the previous frame's operation is a correlation clue, not a proven root cause.
    private string _previousFrameWork = "startup/idle";

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
        // Centre-out rings give the free camera usable nearby geometry before the distant edges.
        // Total pending results are bounded by the selected finite radius (max 11x11 chunks).
        for (int ring = 0; ring <= config.ChunkRadius; ring++)
        for (int x = -ring; x <= ring; x++)
        for (int z = -ring; z <= ring; z++)
        {
            if (Math.Max(Math.Abs(x), Math.Abs(z)) != ring) continue;
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
            if (_previousFrameWork == "mesh") _spikesAfterMesh++;
            else if (_previousFrameWork == "collision") _spikesAfterCollision++;
            else _spikesOther++;
            if (_frameSpikes <= 8)
                GD.Print($"[WORLD PERF] Frame delta spike: {frameMilliseconds:F2} ms " +
                    $"(previous frame operation: {_previousFrameWork}; correlation is NOT causation, " +
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
            _previousFrameWork = "idle";
            if (_completed.TryDequeue(out TerrainChunkData? data))
            { InstallMesh(data); _previousFrameWork = "mesh"; diagnosticsChanged = true; }
            else if (_pendingCollisions.Count > 0)
            { InstallCollision(_pendingCollisions.Dequeue()); _previousFrameWork = "collision"; diagnosticsChanged = true; }
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
            // Observe a short tail after the final collider/mesh work; otherwise the final
            // operation's following frame cannot be counted. Stop automatically once sampled.
            _postLoadFrames++;
            if (_postLoadFrames >= 30)
            {
                PublishStatus();
                GD.Print("[WORLD PERF] FINAL: " + StatusText.Replace("\n", " | "));
                SetProcess(false);
            }
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
        float width = _parameters.ChunkWidth * (2 * _parameters.ChunkRadius + 1);
        StatusText =
            $"Area: {width:F0} x {width:F0} m | Grid: {2 * _parameters.ChunkRadius + 1} x {2 * _parameters.ChunkRadius + 1} chunks\n" +
            $"Chunks: {_attached}/{_total} | Colliders: {_colliders} ({collisionMode}) | mesh step {_parameters.MeshStep}\n" +
            $"Vertices: {_vertices}   Triangles: {_triangles}\n" +
            $"CPU meshing: {_cpuTotalMs:F1} ms (max chunk {_cpuMaxMs:F1})\n" +
            $"Mesh setup: {_meshTotalMs:F1} ms (max chunk {_meshMaxMs:F1})\n" +
            $"Collision cook: {_collisionTotalMs:F1} ms (max chunk {_collisionMaxMs:F1})\n" +
            $"Max observed frame delta: {_maxObservedFrameMs:F1} ms | " +
            $">={_parameters.FrameSpikeThresholdMs:F1} ms spikes: {_frameSpikes}\n" +
            $"Spike correlations: prior mesh {_spikesAfterMesh}, collider {_spikesAfterCollision}, " +
            $"other/startup {_spikesOther}";
        StatisticsChanged?.Invoke(StatusText);
    }
    #endregion
}
