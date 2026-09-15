using LooseLeaf.Abstractions;
using LooseLeaf.Models;

namespace LooseLeaf.Ocr;

public sealed class GoogleDocumentAiOcrProvider : IOcrProvider
{
    public Task<OcrDocument> ProcessAsync(Stream document, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Google Document AI provider is planned but not implemented in this MVP.");
}
