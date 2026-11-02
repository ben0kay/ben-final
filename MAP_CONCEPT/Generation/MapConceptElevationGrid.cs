// Shared MAP2.1 sampled island elevation in normalized u16, independent of 3D terrain.
using Godot;

public sealed class MapConceptElevationGrid
{
    public const int Columns = 129;
    public const int Rows = 89;
    public const string SampleFormat = "u16_normalized";
    public static readonly Vector2 Origin = new(-MapConceptLayout.MapWidth * 0.5f,
        -MapConceptLayout.MapHeight * 0.5f);
    public static readonly Vector2 Step = new(MapConceptLayout.MapWidth / (Columns - 1),
        MapConceptLayout.MapHeight / (Rows - 1));

    // Row-major. Zero is sea level; 65535 represents normalized summit height 1.
    public ushort[] HeightSamples { get; } = new ushort[Columns * Rows];

    // =========================================================
    public void Set(int x, int y, float normalized) =>
        HeightSamples[y * Columns + x] =
            (ushort)Mathf.RoundToInt(Mathf.Clamp(normalized, 0f, 1f) * ushort.MaxValue);

    // =========================================================
    public float At(int x, int y) => HeightSamples[y * Columns + x] / (float)ushort.MaxValue;

    // =========================================================
    public float Sample(Vector2 point)
    {
        float gx = Mathf.Clamp((point.X - Origin.X) / Step.X, 0f, Columns - 1f);
        float gy = Mathf.Clamp((point.Y - Origin.Y) / Step.Y, 0f, Rows - 1f);
        int x = Mathf.Min(Mathf.FloorToInt(gx), Columns - 2);
        int y = Mathf.Min(Mathf.FloorToInt(gy), Rows - 2);
        float tx = gx - x, ty = gy - y;
        return Mathf.Lerp(
            Mathf.Lerp(At(x, y), At(x + 1, y), tx),
            Mathf.Lerp(At(x, y + 1), At(x + 1, y + 1), tx), ty);
    }
}

// A ridge is authored generation data, not a set of decorative mountain icons.
public sealed record ConceptMountainRidge(int Number, Vector2[] Centreline,
    float HalfWidth, float Strength)
{
    public string Id => MapConceptLayout.FeatureId("Mountains", Number);
}
