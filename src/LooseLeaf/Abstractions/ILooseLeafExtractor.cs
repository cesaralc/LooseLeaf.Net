using LooseLeaf.Models;

namespace LooseLeaf.Abstractions;

public interface ILooseLeafExtractor
{
    IDocumentExtractionSession From(string path);

    Task<ExtractionResult<T>> ExtractAsync<T>(
        string path,
        CancellationToken cancellationToken = default);
}
