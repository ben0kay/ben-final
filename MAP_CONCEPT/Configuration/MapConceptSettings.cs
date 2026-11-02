// Typed MAP2.0 controls. Only MAP_CONCEPT owns this resource, not WORLD.
using Godot;

[GlobalClass]
public partial class MapConceptSettings : Resource
{
    [ExportGroup("Procedural Coastline")]
    [Export] public long DefaultSeed { get; set; } = 254279584798L;
    [Export(PropertyHint.Range, "35,75,1")] public int LandCoverage { get; set; } = 60;
    [Export(PropertyHint.Range, "0,100,1")] public int CoastRoughness { get; set; } = 45;
    [Export(PropertyHint.Range, "0,100,1")] public int BaysAndPeninsulas { get; set; } = 55;
    [Export(PropertyHint.Range, "0,8,1")] public int OffshoreIslands { get; set; } = 2;

    // =========================================================
    public MapConceptParameters Snapshot(long seed, int coverage, int roughness, int peninsulas, int islands) =>
        new MapConceptParameters(seed, coverage, roughness, peninsulas, islands).Validated();
}

public readonly record struct MapConceptParameters(long Seed, int LandCoverage,
    int CoastRoughness, int BaysAndPeninsulas, int OffshoreIslands)
{
    public MapConceptParameters Validated() => this with
    {
        LandCoverage = System.Math.Clamp(LandCoverage, 35, 75),
        CoastRoughness = System.Math.Clamp(CoastRoughness, 0, 100),
        BaysAndPeninsulas = System.Math.Clamp(BaysAndPeninsulas, 0, 100),
        OffshoreIslands = System.Math.Clamp(OffshoreIslands, 0, 8)
    };
}
