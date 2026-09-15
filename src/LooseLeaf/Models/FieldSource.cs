namespace LooseLeaf.Models;

public sealed class FieldSource
{
    public int Page { get; init; }

    public string Text { get; init; } = string.Empty;

    public BoundingBox Bounds { get; init; }

    public float OcrConfidence { get; init; }

    public float ExtractionConfidence { get; init; }
}
