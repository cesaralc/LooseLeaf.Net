using LooseLeaf.Models;

namespace LooseLeaf.Abstractions;

public interface IOcrProvider
{
    Task<OcrDocument> ProcessAsync(
        Stream document,
        CancellationToken cancellationToken = default);
}
