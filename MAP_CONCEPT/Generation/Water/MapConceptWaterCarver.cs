// MAP2.2A-R2: classify the final shoreline raster; coastal channels ARE ocean.
// No second centreline carve or painted river mask can contradict the coast.
internal static class MapConceptWaterCarver
{
    public const byte Land = 0;
    public const byte Ocean = 1;
    // Reserved export values for later non-coastal water features.
    public const byte River = 2;
    public const byte Tributary = 3;

    // =========================================================
    public static void Apply(MapConceptMasterElevation master)
    {
        for (int i = 0; i < master.Heights.Length; i++)
            master.WaterMask[i] = master.Heights[i] == 0 ? Ocean : Land;
    }
}
