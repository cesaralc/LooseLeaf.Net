using LooseLeaf.Abstractions;
using LooseLeaf.Models;

namespace LooseLeaf.Core;

public sealed class DocumentExtractionSession : IDocumentExtractionSession
{
    private readonly ILooseLeafExtractor _extractor;
    private readonly string _path;

    public DocumentExtractionSession(ILooseLeafExtractor extractor, string path)
    {
        _extractor = extractor;
        _path = path;
    }

    public Task<ExtractionResult<T>> ExtractAsync<T>(CancellationToken cancellationToken = default) =>
        _extractor.ExtractAsync<T>(_path, cancellationToken);
}
