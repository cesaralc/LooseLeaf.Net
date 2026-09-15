using LooseLeaf.Abstractions;
using LooseLeaf.Models;

namespace LooseLeaf.Ocr;

public sealed class TesseractOcrProvider : IOcrProvider
{
    public Task<OcrDocument> ProcessAsync(Stream document, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Tesseract provider is planned but not implemented in this MVP.");
}
