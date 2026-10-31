// Indexed classic marching cubes; 256-case LUT credited in LOOKUP_TABLE_LICENSE.md.
// Pure managed data generation only: Godot mesh/physics nodes are created on the main thread.
using System;
using System.Collections.Generic;
using Godot;

public sealed class TerrainChunkData
{
    public readonly int ChunkX, ChunkZ;
    public readonly Vector3[] Vertices, Normals;
    public readonly int[] Indices;
    public double GenerationMilliseconds;
    public TerrainChunkData(int x, int z, Vector3[] vertices, Vector3[] normals, int[] indices)
    { ChunkX = x; ChunkZ = z; Vertices = vertices; Normals = normals; Indices = indices; }
}

public static class MarchingCubesMesher
{
    // Table encodes signed 8-bit edge indices: 255 indicates the end of a case.
    // Source: scikit-image CASESCLASSIC, BSD-3-Clause, through Infinigen.
    private static readonly byte[] Triangles = Convert.FromBase64String(@"/////////////////////wAIA/////////////////8AAQn/////////////////AQgDCQgB////
/////////wECCv////////////////8ACAMBAgr/////////////CQIKAAIJ/////////////wII
AwIKCAoJCP////////8DCwL/////////////////AAsCCAsA/////////////wEJAAIDC///////
//////8BCwIBCQsJCAv/////////AwoBCwoD/////////////wAKAQAICggLCv////////8DCQAD
CwkLCgn/////////CQgKCggL/////////////wQHCP////////////////8EAwAHAwT/////////
////AAEJCAQH/////////////wQBCQQHAQcDAf////////8BAgoIBAf/////////////AwQHAwAE
AQIK/////////wkCCgkAAggEB/////////8CCgkCCQcCBwMHCQT/////CAQHAwsC////////////
/wsEBwsCBAIABP////////8JAAEIBAcCAwv/////////BAcLCQQLCQsCCQIB/////wMKAQMLCgcI
BP////////8BCwoBBAsBAAQHCwT/////BAcICQALCQsKCwAD/////wQHCwQLCQkLCv////////8J
BQT/////////////////CQUEAAgD/////////////wAFBAEFAP////////////8IBQQIAwUDAQX/
////////AQIKCQUE/////////////wMACAECCgQJBf////////8FAgoFBAIEAAL/////////AgoF
AwIFAwUEAwQI/////wkFBAIDC/////////////8ACwIACAsECQX/////////AAUEAAEFAgML////
/////wIBBQIFCAIICwQIBf////8KAwsKAQMJBQT/////////BAkFAAgBCAoBCAsK/////wUEAAUA
CwULCgsAA/////8FBAgFCAoKCAv/////////CQcIBQcJ/////////////wkDAAkFAwUHA///////
//8ABwgAAQcBBQf/////////AQUDAwUH/////////////wkHCAkFBwoBAv////////8KAQIJBQAF
AwAFBwP/////CAACCAIFCAUHCgUC/////wIKBQIFAwMFB/////////8HCQUHCAkDCwL/////////
CQUHCQcCCQIAAgcL/////wIDCwABCAEHCAEFB/////8LAgELAQcHAQX/////////CQUICAUHCgED
CgML/////wUHAAUACQcLAAEACgsKAP8LCgALAAMKBQAIAAcFBwD/CwoFBwsF/////////////woG
Bf////////////////8ACAMFCgb/////////////CQABBQoG/////////////wEIAwEJCAUKBv//
//////8BBgUCBgH/////////////AQYFAQIGAwAI/////////wkGBQkABgACBv////////8FCQgF
CAIFAgYDAgj/////AgMLCgYF/////////////wsACAsCAAoGBf////////8AAQkCAwsFCgb/////
////BQoGAQkCCQsCCQgL/////wYDCwYFAwUBA/////////8ACAsACwUABQEFCwb/////AwsGAAMG
AAYFAAUJ/////wYFCQYJCwsJCP////////8FCgYEBwj/////////////BAMABAcDBgUK////////
/wEJAAUKBggEB/////////8KBgUBCQcBBwMHCQT/////BgECBgUBBAcI/////////wECBQUCBgMA
BAMEB/////8IBAcJAAUABgUAAgb/////BwMJBwkEAwIJBQkGAgYJ/wMLAgcIBAoGBf////////8F
CgYEBwIEAgACBwv/////AAEJBAcIAgMLBQoG/////wkCAQkLAgkECwcLBAUKBv8IBAcDCwUDBQEF
Cwb/////BQELBQsGAQALBwsEAAQL/wAFCQAGBQADBgsGAwgEB/8GBQkGCQsEBwkHCwn/////CgQJ
BgQK/////////////wQKBgQJCgAIA/////////8KAAEKBgAGBAD/////////CAMBCAEGCAYEBgEK
/////wEECQECBAIGBP////////8DAAgBAgkCBAkCBgT/////AAIEBAIG/////////////wgDAggC
BAQCBv////////8KBAkKBgQLAgP/////////AAgCAggLBAkKBAoG/////wMLAgABBgAGBAYBCv//
//8GBAEGAQoECAECAQsICwH/CQYECQMGCQEDCwYD/////wgLAQgBAAsGAQkBBAYEAf8DCwYDBgAA
BgT/////////BgQICwYI/////////////wcKBgcICggJCv////////8ABwMACgcACQoGBwr/////
CgYHAQoHAQcIAQgA/////woGBwoHAQEHA/////////8BAgYBBggBCAkIBgf/////AgYJAgkBBgcJ
AAkDBwMJ/wcIAAcABgYAAv////////8HAwIGBwL/////////////AgMLCgYICggJCAYH/////wIA
BwIHCwAJBwYHCgkKB/8BCAABBwgBCgcGBwoCAwv/CwIBCwEHCgYBBgcB/////wgJBggGBwkBBgsG
AwEDBv8ACQELBgf/////////////BwgABwAGAwsACwYA/////wcLBv////////////////8HBgv/
////////////////AwAICwcG/////////////wABCQsHBv////////////8IAQkIAwELBwb/////
////CgECBgsH/////////////wECCgMACAYLB/////////8CCQACCgkGCwf/////////BgsHAgoD
CggDCgkI/////wcCAwYCB/////////////8HAAgHBgAGAgD/////////AgcGAgMHAAEJ////////
/wEGAgEIBgEJCAgHBv////8KBwYKAQcBAwf/////////CgcGAQcKAQgHAQAI/////wADBwAHCgAK
CQYKB/////8HBgoHCggICgn/////////BggECwgG/////////////wMGCwMABgAEBv////////8I
BgsIBAYJAAH/////////CQQGCQYDCQMBCwMG/////wYIBAYLCAIKAf////////8BAgoDAAsABgsA
BAb/////BAsIBAYLAAIJAgoJ/////woJAwoDAgkEAwsDBgQGA/8IAgMIBAIEBgL/////////AAQC
BAYC/////////////wEJAAIDBAIEBgQDCP////8BCQQBBAICBAb/////////CAEDCAYBCAQGBgoB
/////woBAAoABgYABP////////8EBgMEAwgGCgMAAwkKCQP/CgkEBgoE/////////////wQJBQcG
C/////////////8ACAMECQULBwb/////////BQABBQQABwYL/////////wsHBggDBAMFBAMBBf//
//8JBQQKAQIHBgv/////////BgsHAQIKAAgDBAkF/////wcGCwUECgQCCgQAAv////8DBAgDBQQD
AgUKBQILBwb/BwIDBwYCBQQJ/////////wkFBAAIBgAGAgYIB/////8DBgIDBwYBBQAFBAD/////
BgIIBggHAgEIBAgFAQUI/wkFBAoBBgEHBgEDB/////8BBgoBBwYBAAcIBwAJBQT/BAAKBAoFAAMK
BgoHAwcK/wcGCgcKCAUECgQICv////8GCQUGCwkLCAn/////////AwYLAAYDAAUGAAkF/////wAL
CAAFCwABBQUGC/////8GCwMGAwUFAwH/////////AQIKCQULCQsICwUG/////wALAwAGCwAJBgUG
CQECCv8LCAULBQYIAAUKBQIAAgX/BgsDBgMFAgoDCgUD/////wUICQUCCAUGAgMIAv////8JBQYJ
BgAABgL/////////AQUIAQgABQYIAwgCBgII/wEFBgIBBv////////////8BAwYBBgoDCAYFBgkI
CQb/CgEACgAGCQUABQYA/////wADCAUGCv////////////8KBQb/////////////////CwUKBwUL
/////////////wsFCgsHBQgDAP////////8FCwcFCgsBCQD/////////CgcFCgsHCQgBCAMB////
/wsBAgsHAQcFAf////////8ACAMBAgcBBwUHAgv/////CQcFCQIHCQACAgsH/////wcFAgcCCwUJ
AgMCCAkIAv8CBQoCAwUDBwX/////////CAIACAUCCAcFCgIF/////wkAAQUKAwUDBwMKAv////8J
CAIJAgEIBwIKAgUHBQL/AQMFAwcF/////////////wAIBwAHAQEHBf////////8JAAMJAwUFAwf/
////////CQgHBQkH/////////////wUIBAUKCAoLCP////////8FAAQFCwAFCgsLAwD/////AAEJ
CAQKCAoLCgQF/////woLBAoEBQsDBAkEAQMBBP8CBQECCAUCCwgEBQj/////AAQLAAsDBAULAgsB
BQEL/wACBQAFCQILBQQFCAsIBf8JBAUCCwP/////////////AgUKAwUCAwQFAwgE/////wUKAgUC
BAQCAP////////8DCgIDBQoDCAUEBQgAAQn/BQoCBQIEAQkCCQQC/////wgEBQgFAwMFAf//////
//8ABAUBAAX/////////////CAQFCAUDCQAFAAMF/////wkEBf////////////////8ECwcECQsJ
Cgv/////////AAgDBAkHCQsHCQoL/////wEKCwELBAEEAAcEC/////8DAQQDBAgBCgQHBAsKCwT/
BAsHCQsECQILCQEC/////wkHBAkLBwkBCwILAQAIA/8LBwQLBAICBAD/////////CwcECwQCCAME
AwIE/////wIJCgIHCQIDBwcECf////8JCgcJBwQKAgcIBwACAAf/AwcKAwoCBwQKAQoABAAK/wEK
AggHBP////////////8ECQEEAQcHAQP/////////BAkBBAEHAAgBCAcB/////wQAAwcEA///////
//////8ECAf/////////////////CQoICgsI/////////////wMACQMJCwsJCv////////8AAQoA
CggICgv/////////AwEKCwMK/////////////wECCwELCQkLCP////////8DAAkDCQsBAgkCCwn/
////AAILCAAL/////////////wMCC/////////////////8CAwgCCAoKCAn/////////CQoCAAkC
/////////////wIDCAIICgABCAEKCP////8BCgL/////////////////AQMICQEI////////////
/wAJAf////////////////8AAwj//////////////////////////////////////w==");

    // Standard cube corners and the twelve intersectable edges.
    private static readonly int[] CX = { 0, 1, 1, 0, 0, 1, 1, 0 };
    private static readonly int[] CY = { 0, 0, 1, 1, 0, 0, 1, 1 };
    private static readonly int[] CZ = { 0, 0, 0, 0, 1, 1, 1, 1 };
    private static readonly int[] EA = { 0, 1, 2, 3, 4, 5, 6, 7, 0, 1, 2, 3 };
    private static readonly int[] EB = { 1, 2, 3, 0, 5, 6, 7, 4, 4, 5, 6, 7 };

    // =========================================================
    // This first height-derived density field lets us skip empty/solid vertical cells.
    // When true 3D caves are added, use a full 3D density field and volume-aware traversal.
    public static TerrainChunkData Build(TerrainParameters config, DensityGenerator density, int chunkX, int chunkZ)
    {
        // The density remains in world metres; changing mesh step only changes sampling
        // resolution, never the physical chunk width or neighbouring boundary coordinates.
        int n = config.CellsPerChunk / config.MeshStep, side = n + 1;
        float cell = config.CellSize * config.MeshStep, width = config.ChunkWidth;
        float originX = chunkX * width, originZ = chunkZ * width;
        var heights = new float[side * side];
        var sampleNormals = new Vector3[side * side];

        // Height and gradient values are shared by all vertical cells of this x/z column.
        for (int x = 0; x < side; x++)
        for (int z = 0; z < side; z++)
            heights[x * side + z] = density.Height(originX + x * cell, originZ + z * cell);

        // A major CPU saving: reuse height values for interior gradients rather than
        // resampling four expensive noise evaluations per point. For boundary vertices,
        // fetch one outside height per missing side to match neighbouring chunks exactly.
        for (int x = 0; x < side; x++)
        for (int z = 0; z < side; z++)
        {
            float wx = originX + x * cell, wz = originZ + z * cell;
            float left = x == 0 ? density.Height(wx - cell, wz) : heights[(x - 1) * side + z];
            float right = x == n ? density.Height(wx + cell, wz) : heights[(x + 1) * side + z];
            float back = z == 0 ? density.Height(wx, wz - cell) : heights[x * side + z - 1];
            float front = z == n ? density.Height(wx, wz + cell) : heights[x * side + z + 1];
            float invDiameter = 1f / (2f * cell);
            sampleNormals[x * side + z] = new Vector3(
                -(right - left) * invDiameter, 1f, -(front - back) * invDiameter).Normalized();
        }

        var vertices = new List<Vector3>(n * n * 2);
        var normals = new List<Vector3>(n * n * 2);
        var indices = new List<int>(n * n * 6);
        // Exact grid-edge identity avoids duplicating adjacent-cell vertices.
        var edgeCache = new Dictionary<int, int>(n * n * 4);
        Span<float> values = stackalloc float[8];

        for (int x = 0; x < n; x++)
        for (int z = 0; z < n; z++)
        {
            float h00 = heights[x * side + z];
            float h10 = heights[(x + 1) * side + z];
            float h01 = heights[x * side + z + 1];
            float h11 = heights[(x + 1) * side + z + 1];
            float min = MathF.Min(MathF.Min(h00, h10), MathF.Min(h01, h11));
            float max = MathF.Max(MathF.Max(h00, h10), MathF.Max(h01, h11));
            int firstY = Math.Clamp((int)MathF.Floor(min / cell) - 1, 0, n - 1);
            int lastY = Math.Clamp((int)MathF.Ceiling(max / cell) + 1, 0, n - 1);

            for (int y = firstY; y <= lastY; y++)
            {
                float bottom = y * cell, top = (y + 1) * cell;
                values[0] = h00 - bottom; values[1] = h10 - bottom;
                values[2] = h10 - top;    values[3] = h00 - top;
                values[4] = h01 - bottom; values[5] = h11 - bottom;
                values[6] = h11 - top;    values[7] = h01 - top;
                int mask = 0;
                for (int c = 0; c < 8; c++) if (values[c] > 0f) mask |= 1 << c;
                if (mask == 0 || mask == 255) continue;

                int start = mask * 16;
                for (int i = 0; i < 16; i += 3)
                {
                    int e0 = Triangles[start + i];
                    if (e0 == 255) break;
                    int e1 = Triangles[start + i + 1], e2 = Triangles[start + i + 2];
                    int a = VertexForEdge(e0, x, y, z, n, cell, values, sampleNormals, edgeCache, vertices, normals);
                    int b = VertexForEdge(e1, x, y, z, n, cell, values, sampleNormals, edgeCache, vertices, normals);
                    int c = VertexForEdge(e2, x, y, z, n, cell, values, sampleNormals, edgeCache, vertices, normals);
                    // Godot's ArrayMesh front-face winding follows the classic table order for
                    // this corner convention; reversing it made the visible terrain back-facing.
                    // Vertex normals still point outward/up from the signed-density field.
                    indices.Add(a); indices.Add(b); indices.Add(c);
                }
            }
        }
        return new TerrainChunkData(chunkX, chunkZ, vertices.ToArray(), normals.ToArray(), indices.ToArray());
    }

    // =========================================================
    // Cache vertex by canonical lattice edge (first endpoint plus axis).
    private static int VertexForEdge(int edge, int x, int y, int z, int n, float cell, ReadOnlySpan<float> d,
        Vector3[] sampleNormals, Dictionary<int, int> cache, List<Vector3> vertices, List<Vector3> normals)
    {
        int a = EA[edge], b = EB[edge];
        int ax = x + CX[a], ay = y + CY[a], az = z + CZ[a];
        int bx = x + CX[b], by = y + CY[b], bz = z + CZ[b];
        int axis = ax != bx ? 0 : ay != by ? 1 : 2;
        int gx = Math.Min(ax, bx), gy = Math.Min(ay, by), gz = Math.Min(az, bz);
        int side = n + 1;
        int key = (((gx * side + gy) * side + gz) * 3) + axis;
        if (cache.TryGetValue(key, out int existing)) return existing;

        float difference = d[a] - d[b];
        float t = MathF.Abs(difference) < 1e-6f ? 0.5f : Math.Clamp(d[a] / difference, 0f, 1f);
        Vector3 position = new(
            (ax + (bx - ax) * t) * cell,
            (ay + (by - ay) * t) * cell,
            (az + (bz - az) * t) * cell);
        Vector3 na = sampleNormals[ax * side + az], nb = sampleNormals[bx * side + bz];
        Vector3 normal = na.Lerp(nb, t).Normalized();
        int index = vertices.Count;
        vertices.Add(position); normals.Add(normal); cache[key] = index;
        return index;
    }
}
