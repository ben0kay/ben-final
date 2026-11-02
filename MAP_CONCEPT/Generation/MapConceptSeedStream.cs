// SplitMix64 random stream keeps all 64 seed bits and the existing draw sequence.
using System;
using System.Collections.Generic;
using Godot;

internal struct MapConceptSeedStream
{
    private ulong _state;
    public MapConceptSeedStream(ulong seed) => _state = seed;

    // =========================================================
    private ulong Next64()
    {
        unchecked
        {
            ulong x = (_state += 0x9E3779B97F4A7C15UL);
            x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
            x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
            return x ^ (x >> 31);
        }
    }

    public int NextInt() => unchecked((int)Next64());
    public int NextInt(int min, int max) => min + (int)(Next64() % (uint)(max - min));
    public float Range(float min, float max) =>
        min + (float)((Next64() >> 40) / 16777216.0) * (max - min);
}
