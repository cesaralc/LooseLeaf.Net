namespace LooseLeaf.Models;

public sealed class ProcessedDocument
{
    public IReadOnlyList<DocumentPage> Pages { get; init; } = [];

    public bool UsedOcr { get; init; }

    public string Source { get; init; } = string.Empty;
}
