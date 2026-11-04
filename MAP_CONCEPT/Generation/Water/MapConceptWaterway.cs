// BW-MAP2.2A: typed saltwater inlet and freshwater path records, not decorative strokes.
using Godot;

public enum ConceptWaterKind { OceanInlet, River, Tributary }

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
        _ => $"Tributary {Number}"
    };

    // River -> ocean inlet and tributary -> main river links are explicit data.
    public string? ConnectedWaterwayId { get; init; }

    // Saltwater inlets store ocean -> inland; freshwater stores source -> mouth.
    public Vector2 InlandEnd => Kind == ConceptWaterKind.OceanInlet ? Points[^1] : Points[0];
}
