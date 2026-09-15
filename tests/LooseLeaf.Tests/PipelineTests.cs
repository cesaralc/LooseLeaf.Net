using LooseLeaf.Abstractions;
using LooseLeaf.Configuration;
using LooseLeaf.Core;
using LooseLeaf.Models;

namespace LooseLeaf.Tests;

public sealed class PipelineTests
{
    [Fact]
    public async Task AutoMode_UsesNativeText_WhenUsable()
    {
        var native = new StubNativeTextExtractor(
            new NativeTextExtractionResult(
                new ProcessedDocument
                {
                    Source = "native",
                    UsedOcr = false,
                    Pages = [new DocumentPage { PageNumber = 1, Elements = [new DocumentElement { Text = "Total 10" }] }]
                },
                ExtractedCharacterCount: 100));
        var ocr = new StubOcrProvider();
        var structured = new StubStructuredExtractor<Invoice>(new Invoice { InvoiceNumber = "A1", Total = 10m });
        var sut = new LooseLeafExtractor(new LooseLeafOptions(), native, [ocr], structured);

        var path = CreateTempFilePath(".pdf");
        await File.WriteAllTextAsync(path, "fake");
        try
        {
            var result = await sut.ExtractAsync<Invoice>(path);
            Assert.False(result.ProcessedDocument.UsedOcr);
            Assert.Equal(0, ocr.CallCount);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task AutoMode_FallsBackToOcr_WhenNativeTextNotUsable()
    {
        var native = new StubNativeTextExtractor(
            new NativeTextExtractionResult(
                new ProcessedDocument { Source = "native", UsedOcr = false },
                ExtractedCharacterCount: 2));
        var ocr = new StubOcrProvider(new OcrDocument
        {
            Pages = [new OcrPage { PageNumber = 1, Elements = [new OcrElement { Text = "Invoice Total", Confidence = 0.99f }] }]
        });
        var structured = new StubStructuredExtractor<Invoice>(new Invoice { InvoiceNumber = "A1", Total = 20m });
        var sut = new LooseLeafExtractor(new LooseLeafOptions(), native, [ocr], structured);

        var path = CreateTempFilePath(".pdf");
        await File.WriteAllTextAsync(path, "fake");
        try
        {
            var result = await sut.ExtractAsync<Invoice>(path);
            Assert.True(result.ProcessedDocument.UsedOcr);
            Assert.Equal(1, ocr.CallCount);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task NeverMode_ThrowsWhenNativeTextNotUsable()
    {
        var native = new StubNativeTextExtractor(
            new NativeTextExtractionResult(
                new ProcessedDocument { Source = "native", UsedOcr = false },
                ExtractedCharacterCount: 2));
        var ocr = new StubOcrProvider();
        var structured = new StubStructuredExtractor<Invoice>(new Invoice { InvoiceNumber = "A1", Total = 20m });
        var sut = new LooseLeafExtractor(
            new LooseLeafOptions { OcrMode = OcrMode.Never },
            native,
            [ocr],
            structured);

        var path = CreateTempFilePath(".pdf");
        await File.WriteAllTextAsync(path, "fake");
        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ExtractAsync<Invoice>(path));
            Assert.Equal(0, ocr.CallCount);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task AlwaysMode_RunsOcr_EvenWhenNativeTextIsUsable()
    {
        var native = new StubNativeTextExtractor(
            new NativeTextExtractionResult(
                new ProcessedDocument { Source = "native", UsedOcr = false },
                ExtractedCharacterCount: 1000));
        var ocr = new StubOcrProvider();
        var structured = new StubStructuredExtractor<Invoice>(new Invoice { InvoiceNumber = "A1", Total = 20m });
        var sut = new LooseLeafExtractor(
            new LooseLeafOptions { OcrMode = OcrMode.Always },
            native,
            [ocr],
            structured);

        var path = CreateTempFilePath(".pdf");
        await File.WriteAllTextAsync(path, "fake");
        try
        {
            var result = await sut.ExtractAsync<Invoice>(path);
            Assert.True(result.ProcessedDocument.UsedOcr);
            Assert.Equal(1, ocr.CallCount);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task RunOcrAsync_ThrowsWhenNoProviderConfigured()
    {
        var native = new StubNativeTextExtractor(
            new NativeTextExtractionResult(
                new ProcessedDocument { Source = "native", UsedOcr = false },
                ExtractedCharacterCount: 2));
        var structured = new StubStructuredExtractor<Invoice>(new Invoice { InvoiceNumber = "A1", Total = 20m });
        var sut = new LooseLeafExtractor(new LooseLeafOptions(), native, [], structured);

        var path = CreateTempFilePath(".pdf");
        await File.WriteAllTextAsync(path, "fake");
        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ExtractAsync<Invoice>(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ExtractAsync_ThrowsForEmptyPath()
    {
        var native = new StubNativeTextExtractor(null);
        var ocr = new StubOcrProvider();
        var structured = new StubStructuredExtractor<Invoice>(new Invoice());
        var sut = new LooseLeafExtractor(new LooseLeafOptions(), native, [ocr], structured);

        await Assert.ThrowsAsync<ArgumentException>(() => sut.ExtractAsync<Invoice>(string.Empty));
    }

    [Fact]
    public async Task From_CreatesSessionThatDelegatesToExtractor()
    {
        var native = new StubNativeTextExtractor(
            new NativeTextExtractionResult(
                new ProcessedDocument { Source = "native", UsedOcr = false },
                ExtractedCharacterCount: 100));
        var ocr = new StubOcrProvider();
        var structured = new StubStructuredExtractor<Invoice>(new Invoice { InvoiceNumber = "A1", Total = 10m });
        var sut = new LooseLeafExtractor(new LooseLeafOptions(), native, [ocr], structured);

        var path = CreateTempFilePath(".pdf");
        await File.WriteAllTextAsync(path, "fake");
        try
        {
            var result = await sut.From(path).ExtractAsync<Invoice>();
            Assert.Equal("A1", result.Data.InvoiceNumber);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task GetSource_ReturnsFieldProvenance()
    {
        var source = new FieldSource
        {
            Page = 3,
            Text = "Total Due: $12,847.32",
            Bounds = new BoundingBox(.61f, .72f, .30f, .04f),
            OcrConfidence = .997f,
            ExtractionConfidence = .982f
        };

        var native = new StubNativeTextExtractor(
            new NativeTextExtractionResult(
                new ProcessedDocument { Source = "native", UsedOcr = false },
                ExtractedCharacterCount: 100));
        var ocr = new StubOcrProvider();
        var structured = new StubStructuredExtractor<Invoice>(
            new Invoice { InvoiceNumber = "INV-1", Total = 12847.32m },
            new Dictionary<string, FieldSource>(StringComparer.OrdinalIgnoreCase)
            {
                ["Total"] = source
            });
        var sut = new LooseLeafExtractor(new LooseLeafOptions(), native, [ocr], structured);

        var path = CreateTempFilePath(".pdf");
        await File.WriteAllTextAsync(path, "fake");
        try
        {
            var result = await sut.ExtractAsync<Invoice>(path);
            var totalSource = result.GetSource(x => x.Total);
            Assert.NotNull(totalSource);
            Assert.Equal(3, totalSource!.Page);
            Assert.Equal("Total Due: $12,847.32", totalSource.Text);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task GetSource_ReturnsNull_WhenFieldHasNoProvenance()
    {
        var native = new StubNativeTextExtractor(
            new NativeTextExtractionResult(
                new ProcessedDocument { Source = "native", UsedOcr = false },
                ExtractedCharacterCount: 100));
        var ocr = new StubOcrProvider();
        var structured = new StubStructuredExtractor<Invoice>(new Invoice { InvoiceNumber = "A1", Total = 10m });
        var sut = new LooseLeafExtractor(new LooseLeafOptions(), native, [ocr], structured);

        var path = CreateTempFilePath(".pdf");
        await File.WriteAllTextAsync(path, "fake");
        try
        {
            var result = await sut.ExtractAsync<Invoice>(path);
            Assert.Null(result.GetSource(x => x.Total));
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string CreateTempFilePath(string extension) =>
        Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{extension}");

    internal sealed class StubNativeTextExtractor : INativeTextExtractor
    {
        private readonly NativeTextExtractionResult? _result;

        public StubNativeTextExtractor(NativeTextExtractionResult? result) => _result = result;

        public Task<NativeTextExtractionResult?> TryExtractAsync(
            string filePath,
            Stream stream,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_result);
    }

    internal sealed class StubOcrProvider : IOcrProvider
    {
        private readonly OcrDocument _result;
        public int CallCount { get; private set; }

        public StubOcrProvider(OcrDocument? result = null)
        {
            _result = result ?? new OcrDocument
            {
                Pages = [new OcrPage { PageNumber = 1 }]
            };
        }

        public Task<OcrDocument> ProcessAsync(
            Stream document,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(_result);
        }
    }

    internal sealed class StubStructuredExtractor<T> : IStructuredDataExtractor where T : class
    {
        private readonly T _data;
        private readonly IReadOnlyDictionary<string, FieldSource> _sources;

        public StubStructuredExtractor(T data, IReadOnlyDictionary<string, FieldSource>? sources = null)
        {
            _data = data;
            _sources = sources ?? new Dictionary<string, FieldSource>(StringComparer.OrdinalIgnoreCase);
        }

        public Task<StructuredExtractionResult<TModel>> ExtractAsync<TModel>(
            ProcessedDocument document,
            CancellationToken cancellationToken = default)
        {
            if (_data is not TModel typed)
            {
                throw new InvalidOperationException("Unexpected generic model type.");
            }

            return Task.FromResult(new StructuredExtractionResult<TModel>
            {
                Data = typed,
                Sources = _sources
            });
        }
    }

    public sealed class Invoice
    {
        public string InvoiceNumber { get; set; } = string.Empty;

        public decimal Total { get; set; }
    }
}
