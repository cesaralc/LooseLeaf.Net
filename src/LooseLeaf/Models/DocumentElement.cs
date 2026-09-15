namespace LooseLeaf.Models;

public sealed class DocumentElement
{
    public string Text { get; init; } = string.Empty;

    public BoundingBox Bounds { get; init; }

    public float Confidence { get; init; }

    public ElementType Type { get; init; } = ElementType.Text;
}
