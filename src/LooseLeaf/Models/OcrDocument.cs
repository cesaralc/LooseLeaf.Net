namespace LooseLeaf.Models;

public sealed class OcrDocument
{
    public IReadOnlyList<OcrPage> Pages { get; init; } = [];
}

public sealed class OcrPage
{
    public int PageNumber { get; init; }

    public IReadOnlyList<OcrElement> Elements { get; init; } = [];
}

public sealed class OcrElement
{
    public string Text { get; init; } = string.Empty;

    public BoundingBox Bounds { get; init; }

    public float Confidence { get; init; }

    public ElementType Type { get; init; } = ElementType.Text;
}
