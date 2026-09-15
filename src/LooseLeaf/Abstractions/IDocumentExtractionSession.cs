using LooseLeaf.Models;

namespace LooseLeaf.Abstractions;

public interface IDocumentExtractionSession
{
    Task<ExtractionResult<T>> ExtractAsync<T>(
        CancellationToken cancellationToken = default);
}
