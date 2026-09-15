using LooseLeaf.Ocr;

namespace LooseLeaf.Tests;

public sealed class OcrProviderTests
{
    [Fact]
    public async Task TesseractOcrProvider_ThrowsNotSupported()
    {
        var provider = new TesseractOcrProvider();
        await Assert.ThrowsAsync<NotSupportedException>(() => provider.ProcessAsync(Stream.Null));
    }

    [Fact]
    public async Task AwsTextractOcrProvider_ThrowsNotSupported()
    {
        var provider = new AwsTextractOcrProvider();
        await Assert.ThrowsAsync<NotSupportedException>(() => provider.ProcessAsync(Stream.Null));
    }

    [Fact]
    public async Task GoogleDocumentAiOcrProvider_ThrowsNotSupported()
    {
        var provider = new GoogleDocumentAiOcrProvider();
        await Assert.ThrowsAsync<NotSupportedException>(() => provider.ProcessAsync(Stream.Null));
    }

    [Fact]
    public async Task OpenAiVisionOcrProvider_ThrowsNotSupported()
    {
        var provider = new OpenAiVisionOcrProvider();
        await Assert.ThrowsAsync<NotSupportedException>(() => provider.ProcessAsync(Stream.Null));
    }

    [Theory]
    [InlineData("", "key")]
    [InlineData("https://example.com", "")]
    public void AzureDocumentIntelligenceOcrProvider_ThrowsOnMissingArguments(string endpoint, string apiKey)
    {
        Assert.Throws<ArgumentException>(() => new AzureDocumentIntelligenceOcrProvider(endpoint, apiKey));
    }
}
