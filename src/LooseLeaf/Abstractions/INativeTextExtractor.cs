using LooseLeaf.Models;

namespace LooseLeaf.Abstractions;

public interface INativeTextExtractor
{
    Task<NativeTextExtractionResult?> TryExtractAsync(
        string filePath,
        Stream stream,
        CancellationToken cancellationToken = default);
}
