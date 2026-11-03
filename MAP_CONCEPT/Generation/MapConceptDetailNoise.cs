// Fast stable seeded value noise for master-map microdetail at any map coordinate.
// A mountain's local seed can later change without changing its placement or neighbours.
using Godot;

internal static class MapConceptDetailNoise
{
    // =========================================================
    public static float Sample(float x, float y, long seed)
    {
        int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
        float fx = x - ix, fy = y - iy;
        float u = fx * fx * (3f - 2f * fx);
        float v = fy * fy * (3f - 2f * fy);
        uint key = unchecked((uint)(seed ^ (seed >> 32)));
        float a = Hash(ix, iy, key);
        float b = Hash(ix + 1, iy, key);
        float c = Hash(ix, iy + 1, key);
        float d = Hash(ix + 1, iy + 1, key);
        return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
    }

    // =========================================================
    private static float Hash(int x, int y, uint seed)
    {
        uint h = unchecked(seed ^ ((uint)x * 0x9E3779B1u) ^ ((uint)y * 0x85EBCA77u));
        unchecked
        {
            h ^= h >> 16;
            h *= 0x7FEB352Du;
            h ^= h >> 15;
            h *= 0x846CA68Bu;
            h ^= h >> 16;
        }
        return (h / (float)uint.MaxValue) * 2f - 1f;
    }
}
