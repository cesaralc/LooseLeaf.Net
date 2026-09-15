using LooseLeaf.Abstractions;
using LooseLeaf.Configuration;
using LooseLeaf.Models;

namespace LooseLeaf.Core;

public sealed class LooseLeafExtractor : ILooseLeafExtractor
{
    private readonly LooseLeafOptions _options;
    private readonly INativeTextExtractor _nativeTextExtractor;
    private readonly IOcrProvider? _ocrProvider;
    private readonly IStructuredDataExtractor _structuredDataExtractor;

    public LooseLeafExtractor(
        LooseLeafOptions options,
        INativeTextExtractor nativeTextExtractor,
        IEnumerable<IOcrProvider> ocrProviders,
        IStructuredDataExtractor structuredDataExtractor)
    {
        _options = options;
        _nativeTextExtractor = nativeTextExtractor;
        _ocrProvider = ocrProviders.FirstOrDefault();
        _structuredDataExtractor = structuredDataExtractor;
    }

    public IDocumentExtractionSession From(string path) =>
        new DocumentExtractionSession(this, path);

    public async Task<ExtractionResult<T>> ExtractAsync<T>(
        string path,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path is required.", nameof(path));
        }

        await using var stream = File.OpenRead(path);
        var processedDocument = await PrepareDocumentAsync(path, stream, cancellationToken);
        var extraction = await _structuredDataExtractor.ExtractAsync<T>(processedDocument, cancellationToken);
        var validationIssues = ExtractionResult<T>.ValidateData(extraction.Data);

        return new ExtractionResult<T>(
            extraction.Data,
            extraction.Sources,
            validationIssues,
            processedDocument);
    }

    private async Task<ProcessedDocument> PrepareDocumentAsync(
        string filePath,
        Stream document,
        CancellationToken cancellationToken)
    {
        var native = await _nativeTextExtractor.TryExtractAsync(filePath, document, cancellationToken);
        var nativeUsable =
            native is not null &&
            native.ExtractedCharacterCount >= _options.MinimumNativeTextCharacters;

        switch (_options.OcrMode)
        {
            case OcrMode.Never:
                if (!nativeUsable)
                {
                    throw new InvalidOperationException(
                        "OCR is disabled but native text extraction did not produce usable text.");
                }
                return native!.Document;

            case OcrMode.Always:
                return await RunOcrAsync(document, cancellationToken);

            case OcrMode.Auto:
            default:
                if (nativeUsable)
                {
                    return native!.Document;
                }

                return await RunOcrAsync(document, cancellationToken);
        }
    }

    private async Task<ProcessedDocument> RunOcrAsync(Stream document, CancellationToken cancellationToken)
    {
        if (_ocrProvider is null)
        {
            throw new InvalidOperationException(
                "No OCR provider is configured. Configure one (e.g. AddAzureDocumentIntelligence) or set OcrMode.Never.");
        }

        if (document.CanSeek)
        {
            document.Position = 0;
        }

        var ocr = await _ocrProvider.ProcessAsync(document, cancellationToken);

        return new ProcessedDocument
        {
            UsedOcr = true,
            Source = _ocrProvider.GetType().Name,
            Pages = ocr.Pages
                .Select(p => new DocumentPage
                {
                    PageNumber = p.PageNumber,
                    Elements = p.Elements.Select(e => new DocumentElement
                    {
                        Text = e.Text,
                        Bounds = e.Bounds,
                        Confidence = e.Confidence,
                        Type = e.Type
                    }).ToList()
                })
                .ToList()
        };
    }
}
