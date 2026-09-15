using LooseLeaf.Abstractions;
using LooseLeaf.Models;

namespace LooseLeaf.Ocr;

public sealed class OpenAiVisionOcrProvider : IOcrProvider
{
    public Task<OcrDocument> ProcessAsync(Stream document, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("OpenAI vision OCR provider is planned but not implemented in this MVP.");
}
