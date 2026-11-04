// Bounded mainland-only routing grid. Built once before rivers cut any land.
// Coast-distance costs keep long meanders inland instead of following ocean bays.
using System;
using System.Collections.Generic;
using Godot;

internal sealed class MapConceptRiverRouter
{
    private const int Columns = 201, Rows = 136;
    private const float Step = 8f;
    private static readonly Vector2 Origin = new(-800f, -540f);
    private readonly bool[] _land = new bool[Columns * Rows];
    private readonly int[] _clearance = new int[Columns * Rows];
    private readonly MapConceptLayout _mainland;
    // Reuse search buffers for all guides/attempts instead of allocating a
    // complete grid for every A* leg.
    private readonly float[] _cost = new float[Columns * Rows];
    private readonly int[] _parent = new int[Columns * Rows];
    private readonly bool[] _closed = new bool[Columns * Rows];
    private readonly PriorityQueue<int, float> _open = new();
    public Vector2 Core { get; }

    // =========================================================
    public MapConceptRiverRouter(MapConceptLayout mainland)
    {
        _mainland = mainland;
        var queue = new Queue<int>();
        for (int i = 0; i < _land.Length; i++)
        {
            _land[i] = MapConceptLayout.PointInPolygon(Position(i), mainland.Coast);
            _clearance[i] = _land[i] ? int.MaxValue : 0;
            if (!_land[i]) queue.Enqueue(i);
        }
        while (queue.Count > 0)
        {
            int current = queue.Dequeue(), x = current % Columns, y = current / Columns;
            for (int d = 0; d < 4; d++)
            {
                int nx = x + (d == 0 ? -1 : d == 1 ? 1 : 0);
                int ny = y + (d == 2 ? -1 : d == 3 ? 1 : 0);
                if (nx < 0 || nx >= Columns || ny < 0 || ny >= Rows) continue;
                int next = ny * Columns + nx;
                if (_clearance[next] <= _clearance[current] + 1) continue;
                _clearance[next] = _clearance[current] + 1;
                queue.Enqueue(next);
            }
        }
        int deepest = 0;
        for (int i = 0; i < _land.Length; i++)
            if (_land[i] && _clearance[i] > _clearance[deepest]) deepest = i;
        Core = Position(deepest);
    }

    // =========================================================
    public Vector2 Mouth(Vector2 outward)
    {
        Vector2 land = Core, sea = Core;
        for (int i = 1; i <= 500; i++)
        {
            sea = Core + outward * (i * 4f);
            if (!MapConceptLayout.PointInPolygon(sea, _mainland.Coast)) break;
            land = sea;
        }
        for (int i = 0; i < 12; i++)
        {
            Vector2 middle = (land + sea) * 0.5f;
            if (MapConceptLayout.PointInPolygon(middle, _mainland.Coast)) land = middle;
            else sea = middle;
        }
        return (land + sea) * 0.5f;
    }

    // =========================================================
    public Vector2 FarInterior(Vector2 mouth)
    {
        int best = Nearest(Core);
        float score = 0f;
        for (int i = 0; i < _land.Length; i++)
        {
            if (!_land[i] || _clearance[i] < 4) continue;
            float candidate = Position(i).DistanceSquaredTo(mouth);
            if (candidate <= score) continue;
            score = candidate;
            best = i;
        }
        return Position(best);
    }

    // =========================================================
    // Intermediate alternating guides force repeated bends along a long route.
    public List<Vector2> Meander(Vector2 start, Vector2 end, float winding,
        ref MapConceptSeedStream random)
    {
        Vector2 delta = end - start;
        Vector2 normal = new Vector2(-delta.Y, delta.X).Normalized();
        int guides = Mathf.Clamp(Mathf.RoundToInt(delta.Length() / 65f), 3, 12);
        var path = new List<Vector2>();
        int previous = Nearest(start);
        for (int i = 1; i <= guides; i++)
        {
            float t = i / (float)guides;
            float offset = i == guides ? 0f : (i % 2 == 0 ? -1f : 1f) *
                random.Range(20f, 38f + winding * 45f);
            Vector2 guide = start.Lerp(end, t) + normal * offset;
            int next = Nearest(guide);
            List<Vector2> leg = FindPath(previous, next);
            if (leg.Count == 0) return new List<Vector2>();
            for (int j = path.Count == 0 ? 0 : 1; j < leg.Count; j++) path.Add(leg[j]);
            previous = next;
        }
        return path;
    }

    // =========================================================
    private int Nearest(Vector2 point)
    {
        int best = 0;
        float distance = float.MaxValue;
        for (int i = 0; i < _land.Length; i++)
        {
            if (!_land[i] || _clearance[i] < 2) continue;
            float candidate = Position(i).DistanceSquaredTo(point);
            if (candidate >= distance) continue;
            best = i;
            distance = candidate;
        }
        return best;
    }

    // =========================================================
    private List<Vector2> FindPath(int start, int end)
    {
        var open = _open;
        var cost = _cost;
        var parent = _parent;
        var closed = _closed;
        open.Clear();
        Array.Clear(closed, 0, closed.Length);
        Array.Fill(cost, float.PositiveInfinity);
        Array.Fill(parent, -1);
        cost[start] = 0f;
        open.Enqueue(start, 0f);
        while (open.TryDequeue(out int current, out _))
        {
            if (closed[current]) continue;
            if (current == end)
            {
                var path = new List<Vector2>();
                for (int cursor = end; cursor >= 0; cursor = parent[cursor])
                    path.Add(Position(cursor));
                path.Reverse();
                return path;
            }
            closed[current] = true;
            int x = current % Columns, y = current / Columns;
            for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = x + dx, ny = y + dy;
                if (nx <= 0 || nx >= Columns - 1 || ny <= 0 || ny >= Rows - 1) continue;
                int next = ny * Columns + nx;
                if (closed[next] || !_land[next] || _clearance[next] < 2) continue;
                if (dx != 0 && dy != 0 &&
                    (!_land[y * Columns + nx] || !_land[ny * Columns + x])) continue;
                float move = dx != 0 && dy != 0 ? 1.414214f : 1f;
                float nextCost = cost[current] + move * (1f + 3f / _clearance[next]);
                if (nextCost >= cost[next]) continue;
                cost[next] = nextCost;
                parent[next] = current;
                open.Enqueue(next, nextCost + Position(next).DistanceTo(Position(end)) / Step);
            }
        }
        return new List<Vector2>();
    }

    // =========================================================
    private static Vector2 Position(int index) => Origin +
        new Vector2(index % Columns * Step, index / Columns * Step);
}
