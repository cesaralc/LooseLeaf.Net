namespace LooseLeaf.Models;

public sealed class DocumentPage
{
    public int PageNumber { get; init; }

    public List<DocumentElement> Elements { get; init; } = [];
}
