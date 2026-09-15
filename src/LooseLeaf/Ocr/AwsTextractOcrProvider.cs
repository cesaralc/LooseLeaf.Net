using LooseLeaf.Abstractions;
using LooseLeaf.Models;

namespace LooseLeaf.Ocr;

public sealed class AwsTextractOcrProvider : IOcrProvider
{
    public Task<OcrDocument> ProcessAsync(Stream document, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("AWS Textract provider is planned but not implemented in this MVP.");
}
