using LooseLeaf.Abstractions;
using LooseLeaf.Models;
using UglyToad.PdfPig;

namespace LooseLeaf.Core;

public sealed class PdfNativeTextExtractor : INativeTextExtractor
{
    public Task<NativeTextExtractionResult?> TryExtractAsync(
        string filePath,
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        if (!filePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<NativeTextExtractionResult?>(null);
        }

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        using var document = PdfDocument.Open(stream);
        var pages = new List<DocumentPage>();
        var extractedCharacterCount = 0;

        foreach (var page in document.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var elements = new List<DocumentElement>();
            foreach (var word in page.GetWords())
            {
                if (string.IsNullOrWhiteSpace(word.Text))
                {
                    continue;
                }

                extractedCharacterCount += word.Text.Length;
                var bounds = word.BoundingBox;
                var normalized = new BoundingBox(
                    X: (float)(bounds.Left / page.Width),
                    Y: (float)(bounds.Bottom / page.Height),
                    Width: (float)(bounds.Width / page.Width),
                    Height: (float)(bounds.Height / page.Height));

                elements.Add(new DocumentElement
                {
                    Text = word.Text,
                    Bounds = normalized,
                    Confidence = 1.0f,
                    Type = ElementType.Text
                });
            }

            pages.Add(new DocumentPage
            {
                PageNumber = page.Number,
                Elements = elements
            });
        }

        return Task.FromResult<NativeTextExtractionResult?>(new NativeTextExtractionResult(
            new ProcessedDocument
            {
                Pages = pages,
                UsedOcr = false,
                Source = "PdfNativeText"
            },
            extractedCharacterCount));
    }
}
