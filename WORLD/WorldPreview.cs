// Finite terrain preview with bounded CPU generation, safe regeneration and split mesh/physics timing.
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
    public int CurrentChunkRadius => _parameters.ChunkRadius;
    public int CurrentChunksPerSide => _parameters.ChunksPerSide;
    public int CurrentMeshStep => _parameters.MeshStep;

    private const int MaxQueuedResults = 6;
    private const int MaxRemovalPerFrame = 12;

    private ConcurrentQueue<TerrainChunkData> _completed = new();
    private readonly Queue<PendingCollision> _pendingCollisions = new();
    private readonly Queue<Node> _pendingRemoval = new();
    private readonly SemaphoreSlim _workerGate = new(1, 1);
    private CancellationTokenSource? _cancellation;
    private Task? _generator;
    private Node3D _terrainRoot = null!;
    private ShaderMaterial _material = null!;
    private TerrainParameters _parameters;
    private bool _awaitingRegeneration;
    private int _total, _attached, _colliders, _vertices, _triangles, _frameSpikes;
    private int _spikesAfterMesh, _spikesAfterCollision, _spikesOther, _postLoadFrames;
    private double _cpuTotalMs, _cpuMaxMs, _meshTotalMs, _meshMaxMs;
    private double _collisionTotalMs, _collisionMaxMs, _maxObservedFrameMs;
    // The previous frame's operation is diagnostic correlation, not proof of causation.
    private string _previousFrameWork = "startup/idle";

    private readonly record struct PendingCollision(ArrayMesh Mesh, Node3D Chunk, int X, int Z);

    #region Lifecycle and regeneration

    // =========================================================
    public override void _Ready()
    {
        if (Settings == null || TerrainShader == null)
        {
            GD.PushError("[WORLD] Missing world settings or terrain shader.");
            SetProcess(false); return;
        }

        _terrainRoot = GetNode<Node3D>("TerrainRoot");
        _material = new ShaderMaterial { Shader = TerrainShader };
        TerrainParameters config = Settings.Snapshot();
        RegenerateWorld(config.ChunksPerSide, config.MeshStep);
    }

    // =========================================================
    // Called from the debug test panel. No mutation of the shared WorldMaster.tres.
    // Any old work is cancelled; its results belong to an abandoned queue and cannot
    // appear in the new terrain. Old chunk nodes are removed in bounded frame batches.
    public void RegenerateWorld(int chunksPerSide, int meshStep, TerrainParameters? terrainOverride = null)
    {
        if (Settings == null || _terrainRoot == null) return;
        int count = Math.Clamp(chunksPerSide, 1, 100);

        _cancellation?.Cancel();
        _generator = null;
        _completed = new ConcurrentQueue<TerrainChunkData>();
        _pendingCollisions.Clear();

        _parameters = (terrainOverride ?? Settings.Snapshot()) with
        {
            ChunkRadius = count / 2,
            ChunksPerSide = count,
            MeshStep = meshStep is 1 or 2 or 4 ? meshStep : 1
        };
        _total = count * count;
        _attached = _colliders = _vertices = _triangles = _frameSpikes = 0;
        _spikesAfterMesh = _spikesAfterCollision = _spikesOther = _postLoadFrames = 0;
        _cpuTotalMs = _cpuMaxMs = _meshTotalMs = _meshMaxMs = 0;
        _collisionTotalMs = _collisionMaxMs = _maxObservedFrameMs = 0;
        _previousFrameWork = "startup/idle";

        // A second F1 click during cleanup must not queue the same nodes twice.
        if (!_awaitingRegeneration)
        {
            _pendingRemoval.Clear();
            foreach (Node node in _terrainRoot.GetChildren())
                if (!node.IsQueuedForDeletion()) _pendingRemoval.Enqueue(node);
        }

        _awaitingRegeneration = true;
        PublishStatus();
        SetProcess(true);
    }

    // =========================================================
    // Pure managed generation is serialized by one semaphore. Repeated F1 presses
    // cannot leave multiple heavy generators running at once.
    private async Task GenerateAsync(TerrainParameters config, ConcurrentQueue<TerrainChunkData> results,
        CancellationToken token)
    {
        await _workerGate.WaitAsync(token).ConfigureAwait(false);
        try { await Task.Run(() => GenerateTerrain(config, results, token), token).ConfigureAwait(false); }
        finally { _workerGate.Release(); }
    }

    // =========================================================
    // Worker computes nearest chunks first; backpressure limits queued mesh arrays.
    // No Godot resources or scene tree calls occur on the worker.
    private static void GenerateTerrain(TerrainParameters config, ConcurrentQueue<TerrainChunkData> results,
        CancellationToken token)
    {
        var density = new DensityGenerator(config);
        // Works for odd and even NxN grids: e.g. 9 => [-4..4], 10 => [-5..4].
        int minimum = -config.ChunksPerSide / 2;
        int maximum = minimum + config.ChunksPerSide - 1;
        for (int ring = 0; ring <= config.ChunkRadius; ring++)
        for (int x = -ring; x <= ring; x++)
        for (int z = -ring; z <= ring; z++)
        {
            if (Math.Max(Math.Abs(x), Math.Abs(z)) != ring
                || x < minimum || x > maximum || z < minimum || z > maximum) continue;
            token.ThrowIfCancellationRequested();

            var timer = Stopwatch.StartNew();
            TerrainChunkData data = MarchingCubesMesher.Build(config, density, x, z);
            timer.Stop();
            data.GenerationMilliseconds = timer.Elapsed.TotalMilliseconds;

            while (results.Count >= MaxQueuedResults)
            {
                token.ThrowIfCancellationRequested();
                Thread.Sleep(5); // worker-only wait; never block the render thread.
            }
            token.ThrowIfCancellationRequested();
            results.Enqueue(data);
        }
    }

    // =========================================================
    public override void _ExitTree() => _cancellation?.Cancel();
    #endregion

    #region Frame-budgeted installation

    // =========================================================
    public override void _Process(double delta)
    {
        if (_awaitingRegeneration)
        {
            // Limit destruction work while switching between large presets.
            for (int i = 0; i < MaxRemovalPerFrame && _pendingRemoval.Count > 0; i++)
            {
                Node node = _pendingRemoval.Dequeue();
                if (GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion()) node.QueueFree();
            }

            if (_pendingRemoval.Count == 0)
            {
                _awaitingRegeneration = false;
                _cancellation = new CancellationTokenSource();
                CancellationToken token = _cancellation.Token;
                _generator = GenerateAsync(_parameters, _completed, token);
                GD.Print($"[WORLD] Requested {_total} chunks, {_parameters.ChunkWidth * _parameters.ChunksPerSide:F0} m across, mesh step {_parameters.MeshStep}.");
            }

            PublishStatus();
            return;
        }

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
                    $"(previous operation {_previousFrameWork}; correlation, not causation).");
        }

        if (_generator is { IsFaulted: true })
        {
            StatusText = "[WORLD] Terrain generation error: " +
                _generator.Exception?.GetBaseException().Message;
            StatisticsChanged?.Invoke(StatusText);
            GD.PushError(StatusText);
            SetProcess(false);
            return;
        }

        try
        {
            _previousFrameWork = "idle";
            if (_completed.TryDequeue(out TerrainChunkData? data))
            {
                InstallMesh(data);
                _previousFrameWork = "mesh";
                diagnosticsChanged = true;
            }
            else if (_pendingCollisions.Count > 0)
            {
                InstallCollision(_pendingCollisions.Dequeue());
                _previousFrameWork = "collision";
                diagnosticsChanged = true;
            }
        }
        catch (Exception error)
        {
            GD.PushError("[WORLD] Chunk installation failed: " + error);
            StatusText = "Terrain installation error. See Godot Output.";
            StatisticsChanged?.Invoke(StatusText);
            _cancellation?.Cancel();
            SetProcess(false); return;
        }

        if (diagnosticsChanged) PublishStatus();

        if (_generator is { IsCompleted: true } && _completed.IsEmpty && _pendingCollisions.Count == 0)
        {
            // Include the frame after the last work item, then become dormant.
            if (++_postLoadFrames >= 30)
            {
                PublishStatus();
                GD.Print("[WORLD PERF] FINAL: " + StatusText.Replace("\n", " | "));
                SetProcess(false);
            }
        }
    }

    // =========================================================
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

            var chunk = new Node3D
            {
                Name = $"Chunk_{data.ChunkX}_{data.ChunkZ}",
                Position = new Vector3(data.ChunkX * _parameters.ChunkWidth, 0f,
                    data.ChunkZ * _parameters.ChunkWidth)
            };
            _terrainRoot.AddChild(chunk);
            chunk.AddChild(new MeshInstance3D
                { Name = "Surface", Mesh = mesh, MaterialOverride = _material });

            if (addCollision)
                _pendingCollisions.Enqueue(new PendingCollision(mesh, chunk, data.ChunkX, data.ChunkZ));
        }
        else addCollision = false;

        timer.Stop();
        double meshMs = timer.Elapsed.TotalMilliseconds;
        _attached++; _vertices += data.Vertices.Length; _triangles += data.Indices.Length / 3;
        _cpuTotalMs += data.GenerationMilliseconds;
        _cpuMaxMs = Math.Max(_cpuMaxMs, data.GenerationMilliseconds);
        _meshTotalMs += meshMs; _meshMaxMs = Math.Max(_meshMaxMs, meshMs);

        // Only the first few chunks are logged at scale; slow operations remain visible.
        if (_attached <= 3 || _attached == _total || meshMs >= _parameters.MainThreadWarningMs)
            GD.Print($"[WORLD PERF] Chunk ({data.ChunkX},{data.ChunkZ}): CPU " +
                $"{data.GenerationMilliseconds:F2} ms | mesh setup {meshMs:F2} ms | " +
                $"{data.Indices.Length / 3} triangles | physics {(addCollision ? "queued" : "skipped")}");
    }

    // =========================================================
    private void InstallCollision(PendingCollision pending)
    {
        var timer = Stopwatch.StartNew();
        var staticBody = new StaticBody3D { Name = "TerrainCollision" };
        pending.Chunk.AddChild(staticBody);
        staticBody.AddChild(new CollisionShape3D
            { Name = "Shape", Shape = pending.Mesh.CreateTrimeshShape() });
        timer.Stop();

        double ms = timer.Elapsed.TotalMilliseconds;
        _colliders++;
        _collisionTotalMs += ms; _collisionMaxMs = Math.Max(_collisionMaxMs, ms);
        if (ms >= _parameters.MainThreadWarningMs)
            GD.Print($"[WORLD PERF] Slow collision ({pending.X},{pending.Z}): {ms:F2} ms.");
    }
    #endregion

    #region HUD diagnostics

    // =========================================================
    private void PublishStatus()
    {
        string collision = !_parameters.GenerateCollision || _parameters.CollisionChunkRadius < 0
            ? "none" : $"radius {_parameters.CollisionChunkRadius}";
        float size = _parameters.ChunkWidth * _parameters.ChunksPerSide;
        StatusText =
            $"World: {size:F0} x {size:F0} m | {_parameters.ChunksPerSide} x " +
            $"{_parameters.ChunksPerSide} chunks | step {_parameters.MeshStep}\n" +
            $"Chunks: {_attached}/{_total} | Colliders: {_colliders} ({collision}) " +
            $"| queued meshes: {_completed.Count}/{MaxQueuedResults}" +
            (_awaitingRegeneration ? $" | clearing: {_pendingRemoval.Count}" : "") + "\n" +
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
