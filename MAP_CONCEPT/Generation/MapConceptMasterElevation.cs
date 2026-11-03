// BW-MAP2.1B: 4096-square master elevation and cached high-detail shaded relief.
// Independent of Godot WORLD terrain; generated only on map regeneration.
using System;
using Godot;

public sealed class MapConceptMasterElevation : IDisposable
{
    public const int Resolution = 4096;
    public const int TileSize = 256;
    public const float WorldMetres = 12000f;
    public const float CanvasUnits = 1600f;
    public const string RawFile = "map_concept_master_4096.r16";
    public const string SampleFormat = "r16_le_normalized";

    public static readonly Vector2 Origin = new(-CanvasUnits * 0.5f, -CanvasUnits * 0.5f);
    public const float Step = CanvasUnits / (Resolution - 1f);
    public const float MetresPerSample = WorldMetres / (Resolution - 1f);
    public readonly ushort[] Heights = new ushort[Resolution * Resolution];
    public ImageTexture? ReliefTexture { get; internal set; }
    public Rect2 MapRect => new(Origin, new Vector2(CanvasUnits, CanvasUnits));

    // =========================================================
    public float Sample(Vector2 position)
    {
        float gx = Mathf.Clamp((position.X - Origin.X) / Step, 0f, Resolution - 1f);
        float gy = Mathf.Clamp((position.Y - Origin.Y) / Step, 0f, Resolution - 1f);
        int x = Mathf.Min(Mathf.FloorToInt(gx), Resolution - 2);
        int y = Mathf.Min(Mathf.FloorToInt(gy), Resolution - 2);
        int i = y * Resolution + x;
        return Mathf.Lerp(
            Mathf.Lerp(Heights[i], Heights[i + 1], gx - x),
            Mathf.Lerp(Heights[i + Resolution], Heights[i + Resolution + 1], gx - x), gy - y) /
            ushort.MaxValue;
    }

    // =========================================================
    // Exact little-endian R16 export, 32 MiB per file. Export is opt-in.
    public bool SaveRaw(string userPath)
    {
        if (!BitConverter.IsLittleEndian)
            throw new PlatformNotSupportedException("R16 export requires little-endian output.");
        using var file = FileAccess.Open(userPath, FileAccess.ModeFlags.Write);
        if (file == null) return false;
        var bytes = new byte[Heights.Length * sizeof(ushort)];
        Buffer.BlockCopy(Heights, 0, bytes, 0, bytes.Length);
        file.StoreBuffer(bytes);
        file.Flush();
        return true;
    }

    // =========================================================
    public void Dispose()
    {
        ReliefTexture?.Dispose();
        ReliefTexture = null;
    }
}
