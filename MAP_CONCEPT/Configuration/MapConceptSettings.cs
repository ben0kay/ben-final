// Typed MAP2.0B landmass controls. Only MAP_CONCEPT owns this resource.
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

    [ExportGroup("MAP2.0B — Landmass & Ocean")]
    [Export(PropertyHint.Range, "0,100,1")] public int LandmassComplexity { get; set; } = 65;
    [Export(PropertyHint.Range, "0,100,1")] public int OceanPenetration { get; set; } = 65;

    [ExportGroup("MAP2.1 — Mountains")]
    [Export(PropertyHint.Range, "0,8,1")] public int MinMountainRegions { get; set; } = 1;
    [Export(PropertyHint.Range, "0,8,1")] public int MaxMountainRegions { get; set; } = 5;
    [Export(PropertyHint.Range, "0,100,1")] public int MountainCoverage { get; set; } = 25;
    [Export(PropertyHint.Range, "0,100,1")] public int MountainRuggedness { get; set; } = 60;
    [Export(PropertyHint.Range, "0,100,1")] public int MountainRidgeLength { get; set; } = 55;

    // =========================================================
    public MapConceptParameters Snapshot(long seed, int coverage, int roughness,
        int peninsulas, int islands, int complexity, int penetration,
        int minMountains, int maxMountains, int mountainCoverage,
        int ruggedness, int ridgeLength) =>
        new MapConceptParameters(seed, coverage, roughness, peninsulas,
            islands, complexity, penetration, minMountains, maxMountains,
            mountainCoverage, ruggedness, ridgeLength).Validated();
}

public readonly record struct MapConceptParameters(long Seed, int LandCoverage,
    int CoastRoughness, int BaysAndPeninsulas, int OffshoreIslands,
    int LandmassComplexity, int OceanPenetration,
    int MinMountainRegions, int MaxMountainRegions,
    int MountainCoverage, int MountainRuggedness, int MountainRidgeLength)
{
    public MapConceptParameters Validated() => this with
    {
        LandCoverage = System.Math.Clamp(LandCoverage, 35, 75),
        CoastRoughness = System.Math.Clamp(CoastRoughness, 0, 100),
        BaysAndPeninsulas = System.Math.Clamp(BaysAndPeninsulas, 0, 100),
        OffshoreIslands = System.Math.Clamp(OffshoreIslands, 0, 8),
        LandmassComplexity = System.Math.Clamp(LandmassComplexity, 0, 100),
        OceanPenetration = System.Math.Clamp(OceanPenetration, 0, 100),
        MinMountainRegions = System.Math.Clamp(MinMountainRegions, 0, 8),
        MaxMountainRegions = System.Math.Clamp(MaxMountainRegions,
            System.Math.Clamp(MinMountainRegions, 0, 8), 8),
        MountainCoverage = System.Math.Clamp(MountainCoverage, 0, 100),
        MountainRuggedness = System.Math.Clamp(MountainRuggedness, 0, 100),
        MountainRidgeLength = System.Math.Clamp(MountainRidgeLength, 0, 100)
    };
}
