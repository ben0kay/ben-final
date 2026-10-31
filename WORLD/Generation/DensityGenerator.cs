// Deterministic, region-composed world-space geography: valley, rolling foothills and mountain range.
using System;
using Godot;

public sealed class DensityGenerator
{
    private readonly TerrainParameters _settings;
    public DensityGenerator(TerrainParameters settings) => _settings = settings;

    #region Surface height and normals

    // =========================================================
    // Negative Z is north/ahead from the default fly camera.
    // Lowlands occupy the foreground; rolling hills grow along the sides, while
    // the far north contains an irregular, coherent ridge with sharp subsidiary peaks.
    // All blend masks are in WORLD metres, so the design crosses chunks seamlessly.
    public float Height(float x, float z)
    {
        int seed = _settings.Seed;
        float macroWarpX = Noise(x * 0.005f, z * 0.005f, seed + 187) * 26f;
        float macroWarpZ = Noise(x * 0.005f, z * 0.005f, seed + 283) * 26f;
        float sx = x + macroWarpX, sz = z + macroWarpZ;

        // Lowland meadows get gentle undulations, with almost no micro-noise.
        float lowland = _settings.BaseHeight
            + Fractal(sx * 0.009f, sz * 0.009f, seed + 1) * _settings.BroadAmplitude
            + Fractal(sx * 0.035f, sz * 0.035f, seed + 2) * _settings.DetailAmplitude;

        // Winding central valley/corridor creates reasonably flat traversable space.
        float centreX = 22f * MathF.Sin(z * 0.008f + seed * 0.011f);
        float sideDistance = MathF.Abs(x - centreX);
        float valleyEdge = SmoothStep(_settings.ValleyHalfWidth * 0.55f,
            _settings.ValleyHalfWidth * 1.75f, sideDistance);
        float hillNorthward = SmoothStep(-80f, 120f, -z);
        float hillMask = Math.Clamp(valleyEdge * (0.75f + hillNorthward * 0.25f)
            + hillNorthward * 0.2f, 0f, 1f);
        float hillShape = 0.45f + 0.55f * Fractal(sx * 0.013f, sz * 0.014f, seed + 13);
        float foothills = MathF.Max(0f, hillShape) * _settings.RidgeAmplitude * hillMask;

        // One broad mountain chain on the far (negative Z) horizon plus flanking
        // mountain shoulders. A slight wandering boundary avoids a straight wall.
        float rangeBoundaryOffset = Noise(sx * 0.009f, 0.25f, seed + 31) * 24f;
        float depth = -z + rangeBoundaryOffset;
        float northernRange = SmoothStep(-_settings.MountainStartZ,
            -_settings.MountainFullZ, depth);
        float flanks = SmoothStep(_settings.ValleyHalfWidth,
            _settings.ValleyHalfWidth + 135f, sideDistance)
            * SmoothStep(-80f, 130f, -z) * 0.55f;
        float mountainMask = Math.Clamp(MathF.Max(northernRange, flanks), 0f, 1f);
        mountainMask *= mountainMask * (3f - 2f * mountainMask);

        // Broad tectonic uplift first: a believable mass instead of isolated needles.
        float massNoise = Fractal(sx * 0.011f, sz * 0.011f, seed + 71);
        float mountainMass = _settings.MountainAmplitude * mountainMask
            * (0.55f + 0.25f * massNoise);

        // Narrow ridged noise shapes sawtooth crests across a connected range.
        // A second octave introduces crags, but only inside mountainous regions.
        float ridgeCoordinate = sx + sz * 0.28f;
        float ridgeNoise = Fractal(ridgeCoordinate * 0.029f, sz * 0.018f, seed + 91);
        float mainCrest = MathF.Pow(Math.Clamp(1f - MathF.Abs(ridgeNoise), 0f, 1f),
            _settings.PeakSharpness);
        float smallCrest = MathF.Pow(Math.Clamp(1f - MathF.Abs(
            Noise(sx * 0.066f, sz * 0.059f, seed + 103)), 0f, 1f), 4f);
        float peakHeight = _settings.PeakAmplitude * mountainMask
            * (mainCrest * (0.75f + 0.25f * smallCrest));

        return lowland + foothills + mountainMass + peakHeight;
    }

    // =========================================================
    // Positive density is solid. The same contract can later be extended to 3D caves.
    public float Sample(float x, float y, float z) => Height(x, z) - y;

    // =========================================================
    public Vector3 SurfaceNormal(float x, float z, float step)
    {
        float dx = (Height(x + step, z) - Height(x - step, z)) / (2f * step);
        float dz = (Height(x, z + step) - Height(x, z - step)) / (2f * step);
        return new Vector3(-dx, 1f, -dz).Normalized();
    }
    #endregion

    #region Managed deterministic noise

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
