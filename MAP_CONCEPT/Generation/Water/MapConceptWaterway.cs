// Typed coastal-corridor paths, shared junctions and termination metadata.
using Godot;

public enum ConceptWaterKind { OceanInlet, River, Tributary }
public enum ConceptWaterEnding { None, ConnectedBasin, OppositeCoast, RejoinsChannel }

public sealed record ConceptWaterway(ConceptWaterKind Kind, int Number,
    Vector2[] Points, float[] HalfWidths, float[] SurfaceLevels)
{
    public string Id => MapConceptLayout.FeatureId(Kind switch
    {
        ConceptWaterKind.OceanInlet => "OceanInlet",
        ConceptWaterKind.River => "River",
        _ => "Tributary"
    }, Number);

    public string Name => Kind switch
    {
        ConceptWaterKind.OceanInlet => $"Ocean Inlet {Number}",
        ConceptWaterKind.River => $"River {Number}",
        _ => CoastalChannel ? $"Channel Branch {Number}" : $"Tributary {Number}"
    };

    // Coastal corridors cut the land field; they are all sea-level water and
    // must never be repainted/carved as an independent freshwater stroke.
    public bool CoastalChannel { get; init; }
    public ConceptWaterEnding Ending { get; init; }

    // River -> ocean inlet and tributary -> main river links are explicit data.
    public string? ConnectedWaterwayId { get; init; }
    public string? RejoinsWaterwayId { get; init; }

    // Coastal paths store ocean -> inland; future freshwater uses source -> mouth.
    public Vector2 InlandEnd => CoastalChannel || Kind == ConceptWaterKind.OceanInlet ? Points[^1] : Points[0];
}
