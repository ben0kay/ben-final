// Deterministic world-space terrain density: positive solid, negative air, zero surface.
using System;
using Godot;

public sealed class DensityGenerator
{
    private readonly TerrainParameters _settings;
    public DensityGenerator(TerrainParameters settings) => _settings = settings;

    // =========================================================
    // Continuous hills, medium detail and a broad masked ridge region; no per-chunk randomness.
    public float Height(float x, float z)
    {
        int seed = _settings.Seed;
        float warpX = Noise(x * 0.008f, z * 0.008f, seed + 187) * 9f;
        float warpZ = Noise(x * 0.008f, z * 0.008f, seed + 283) * 9f;
        float sx = x + warpX, sz = z + warpZ;
        float broad = Fractal(sx * 0.014f, sz * 0.014f, seed + 1);
        float detail = Fractal(sx * 0.048f, sz * 0.048f, seed + 2);
        float ridge = 1f - MathF.Abs(Fractal(sx * 0.027f, sz * 0.027f, seed + 3));
        float ridgeMask = SmoothStep(-0.35f, 0.55f, Noise(sx * 0.01f + 12f, sz * 0.01f, seed + 4));
        return _settings.BaseHeight + _settings.BroadAmplitude * broad
            + _settings.DetailAmplitude * detail
            + _settings.RidgeAmplitude * ridgeMask * ridge * ridge;
    }

    // =========================================================
    // The same 3D signed-density contract will later support caves and overhangs.
    public float Sample(float x, float y, float z) => Height(x, z) - y;

    // =========================================================
    // Matching world-coordinate gradients at chunk edges eliminate visible shading seams.
    public Vector3 SurfaceNormal(float x, float z, float step)
    {
        float dx = (Height(x + step, z) - Height(x - step, z)) / (2f * step);
        float dz = (Height(x, z + step) - Height(x, z - step)) / (2f * step);
        return new Vector3(-dx, 1f, -dz).Normalized();
    }

    #region Managed value noise

    // =========================================================
    private static float Fractal(float x, float z, int seed)
    {
        float result = 0f, strength = 0.55f, freq = 1f;
        for (int octave = 0; octave < 3; octave++)
        {
            result += Noise(x * freq, z * freq, seed + octave * 97) * strength;
            strength *= 0.5f; freq *= 2f;
        }
        return result;
    }

    // =========================================================
    private static float Noise(float x, float z, int seed)
    {
        int ix = (int)MathF.Floor(x), iz = (int)MathF.Floor(z);
        float fx = x - ix, fz = z - iz;
        float u = fx * fx * (3f - 2f * fx), v = fz * fz * (3f - 2f * fz);
        float a = Mix(Hash(ix, iz, seed), Hash(ix + 1, iz, seed), u);
        float b = Mix(Hash(ix, iz + 1, seed), Hash(ix + 1, iz + 1, seed), u);
        return Mix(a, b, v);
    }

    // =========================================================
    private static float Hash(int x, int z, int seed)
    {
        unchecked
        {
            uint value = (uint)x * 374761393u + (uint)z * 668265263u + (uint)seed * 2246822519u;
            value ^= value >> 13; value *= 1274126177u; value ^= value >> 16;
            return (value & 0xFFFFFFu) * (2f / 16777215f) - 1f;
        }
    }

    // =========================================================
    private static float Mix(float a, float b, float amount) => a + (b - a) * amount;

    // =========================================================
    private static float SmoothStep(float low, float high, float value)
    {
        float t = Math.Clamp((value - low) / (high - low), 0f, 1f);
        return t * t * (3f - 2f * t);
    }
    #endregion
}
