// Connected marching-squares coast contours and geometry-preserving simplification.
using System;
using System.Collections.Generic;
using Godot;

internal static class MapConceptCoastlineContours
{
    private const float HalfWidth = MapConceptLayout.MapWidth * 0.5f;
    private const float HalfHeight = MapConceptLayout.MapHeight * 0.5f;
    private const float Spacing = 8f;
    private const int GridWidth = 201;
    private const int GridHeight = 136;

    #region Marching squares and connected coast contours

    // =========================================================
    internal static float[] SampleGrid(MapConceptLandField field)
    {
        var grid = new float[GridWidth * GridHeight];
        for (int y = 0; y < GridHeight; y++)
            for (int x = 0; x < GridWidth; x++)
                grid[y * GridWidth + x] = field.Sample(new Vector2(
                    -HalfWidth + x * Spacing, -HalfHeight + y * Spacing));
        return grid;
    }

    // =========================================================
    // Shared grid-edge IDs ensure contours join exactly across adjacent cells;
    // ambiguous diagonal cells are resolved using a centre-sample sign.
    internal static List<Vector2[]> ExtractLandContours(float[] values, MapConceptLandField field)
    {
        int horizontalCount = GridHeight * (GridWidth - 1);
        var ids = new Dictionary<int, int>();
        var vertices = new List<Vector2>();
        var adjacency = new Dictionary<int, List<int>>();
        for (int y = 0; y < GridHeight - 1; y++)
        for (int x = 0; x < GridWidth - 1; x++)
        {
            float a = values[y * GridWidth + x];
            float b = values[y * GridWidth + x + 1];
            float c = values[(y + 1) * GridWidth + x + 1];
            float d = values[(y + 1) * GridWidth + x];
            bool[] land = { a > 0f, b > 0f, c > 0f, d > 0f };
            int mask = (land[0] ? 1 : 0) | (land[1] ? 2 : 0) |
                (land[2] ? 4 : 0) | (land[3] ? 8 : 0);
            if (mask == 0 || mask == 15) continue;

            int[] edgeIds = { y * (GridWidth - 1) + x,
                horizontalCount + y * GridWidth + x + 1,
                (y + 1) * (GridWidth - 1) + x,
                horizontalCount + y * GridWidth + x };
            float[] corners = { a, b, c, d };
            Vector2[] positions = {
                new(-HalfWidth + x * Spacing, -HalfHeight + y * Spacing),
                new(-HalfWidth + (x + 1) * Spacing, -HalfHeight + y * Spacing),
                new(-HalfWidth + (x + 1) * Spacing, -HalfHeight + (y + 1) * Spacing),
                new(-HalfWidth + x * Spacing, -HalfHeight + (y + 1) * Spacing)
            };
            int[] edgeVertices = { -1, -1, -1, -1 };
            int count = 0;
            for (int edge = 0; edge < 4; edge++)
            {
                int next = (edge + 1) % 4;
                if (land[edge] == land[next]) continue;
                int key = edgeIds[edge];
                if (!ids.TryGetValue(key, out int id))
                {
                    float t = Mathf.Clamp(corners[edge] /
                        (corners[edge] - corners[next]), 0f, 1f);
                    id = vertices.Count;
                    vertices.Add(positions[edge].Lerp(positions[next], t));
                    ids.Add(key, id);
                }
                edgeVertices[edge] = id;
                count++;
            }

            if (count == 2)
            {
                int first = -1;
                for (int i = 0; i < 4; i++)
                    if (edgeVertices[i] >= 0)
                    {
                        if (first < 0) first = edgeVertices[i];
                        else Connect(first, edgeVertices[i], adjacency);
                    }
            }
            else if (count == 4)
            {
                bool centerLand = field.Sample(positions[0] +
                    new Vector2(Spacing * 0.5f, Spacing * 0.5f)) > 0f;
                for (int i = 0; i < 4; i++)
                    if (land[i] != centerLand)
                        Connect(edgeVertices[(i + 3) % 4], edgeVertices[i], adjacency);
            }
        }

        var loops = new List<Vector2[]>();
        var seen = new HashSet<int>();
        foreach (int start in adjacency.Keys)
        {
            if (seen.Contains(start)) continue;
            int cursor = start, previous = -1;
            var points = new List<Vector2>();
            bool closed = false;
            for (int i = 0; i < vertices.Count; i++)
            {
                if (!adjacency.TryGetValue(cursor, out var neighbours) ||
                    neighbours.Count != 2 || seen.Contains(cursor)) break;
                seen.Add(cursor);
                points.Add(vertices[cursor]);
                int next = neighbours[0] == previous ? neighbours[1] : neighbours[0];
                if (next == start) { closed = true; break; }
                previous = cursor;
                cursor = next;
            }
            if (!closed || points.Count < 9 || Mathf.Abs(MapConceptGeometry.SignedArea(points)) < 500f)
                continue;

            // Distinguish exterior land loops from interior water holes. The
            // latter are left for the future separate lake/rivers stage.
            float signedArea = MapConceptGeometry.SignedArea(points);
            Vector2 tangent = points[1] - points[0];
            Vector2 side = signedArea > 0f ?
                new Vector2(-tangent.Y, tangent.X) :
                new Vector2(tangent.Y, -tangent.X);
            Vector2 insidePoint = (points[0] + points[1]) * 0.5f +
                side.Normalized() * 2.5f;
            if (field.Sample(insidePoint) <= 0f) continue;

            Vector2[] simplified = SimplifyContour(points, 3.1f);
            if (simplified.Length >= 8) loops.Add(simplified);
        }
        return loops;
    }

    // =========================================================
    private static void Connect(int a, int b, Dictionary<int, List<int>> graph)
    {
        if (a < 0 || b < 0 || a == b) return;
        if (!graph.TryGetValue(a, out var left)) graph[a] = left = new List<int>(2);
        if (!graph.TryGetValue(b, out var right)) graph[b] = right = new List<int>(2);
        if (!left.Contains(b)) left.Add(b);
        if (!right.Contains(a)) right.Add(a);
    }

    // =========================================================
    // Two open RDP arcs preserve closed-loop corners while eliminating hundreds
    // of near-collinear marching-squares vertices before CanvasItem drawing.
    private static Vector2[] SimplifyContour(List<Vector2> ring, float tolerance)
    {
        int split = 1;
        float best = 0f;
        for (int i = 1; i < ring.Count; i++)
        {
            float distance = ring[0].DistanceSquaredTo(ring[i]);
            if (distance > best) { best = distance; split = i; }
        }
        var first = ring.GetRange(0, split + 1);
        var second = ring.GetRange(split, ring.Count - split);
        second.Add(ring[0]);
        List<Vector2> a = SimplifyOpen(first, tolerance);
        List<Vector2> b = SimplifyOpen(second, tolerance);
        a.RemoveAt(a.Count - 1);
        b.RemoveAt(b.Count - 1);
        a.AddRange(b);
        return a.ToArray();
    }

    // =========================================================
    private static List<Vector2> SimplifyOpen(List<Vector2> line, float tolerance)
    {
        var keep = new bool[line.Count];
        keep[0] = keep[^1] = true;
        var stack = new Stack<(int Start, int End)>();
        stack.Push((0, line.Count - 1));
        float epsilonSquared = tolerance * tolerance;
        while (stack.Count > 0)
        {
            var range = stack.Pop();
            Vector2 a = line[range.Start], b = line[range.End];
            Vector2 segment = b - a;
            float lengthSq = segment.LengthSquared();
            int farthest = -1;
            float maxDistanceSq = epsilonSquared;
            for (int i = range.Start + 1; i < range.End; i++)
            {
                float t = lengthSq <= 0.0001f ? 0f :
                    Mathf.Clamp((line[i] - a).Dot(segment) / lengthSq, 0f, 1f);
                float dist = line[i].DistanceSquaredTo(a + t * segment);
                if (dist > maxDistanceSq) { maxDistanceSq = dist; farthest = i; }
            }
            if (farthest < 0) continue;
            keep[farthest] = true;
            stack.Push((range.Start, farthest));
            stack.Push((farthest, range.End));
        }
        var result = new List<Vector2>();
        for (int i = 0; i < line.Count; i++)
            if (keep[i]) result.Add(line[i]);
        return result;
    }
    #endregion
}
