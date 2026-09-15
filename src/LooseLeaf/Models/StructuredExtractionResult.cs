namespace LooseLeaf.Models;

public sealed class StructuredExtractionResult<T>
{
    public required T Data { get; init; }

    public IReadOnlyDictionary<string, FieldSource> Sources { get; init; } =
        new Dictionary<string, FieldSource>(StringComparer.OrdinalIgnoreCase);
}
