using LooseLeaf.Models;

namespace LooseLeaf.Abstractions;

public interface IStructuredDataExtractor
{
    Task<StructuredExtractionResult<T>> ExtractAsync<T>(
        ProcessedDocument document,
        CancellationToken cancellationToken = default);
}
