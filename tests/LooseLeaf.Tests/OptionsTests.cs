using LooseLeaf.Abstractions;
using LooseLeaf.Configuration;
using LooseLeaf.Core;
using LooseLeaf.Models;
using LooseLeaf.Ocr;
using Microsoft.Extensions.DependencyInjection;

namespace LooseLeaf.Tests;

public sealed class OptionsTests
{
    [Fact]
    public void OcrMode_DefaultsToAuto()
    {
        var options = new LooseLeafOptions();
        Assert.Equal(OcrMode.Auto, options.OcrMode);
    }

    [Fact]
    public void AddOcrProvider_ThrowsOnNull()
    {
        var options = new LooseLeafOptions();
        Assert.Throws<ArgumentNullException>(() => options.AddOcrProvider(null!));
    }

    [Fact]
    public void AddChatClient_ThrowsOnNull()
    {
        var options = new LooseLeafOptions();
        Assert.Throws<ArgumentNullException>(() => options.AddChatClient(null!));
    }

    [Theory]
    [InlineData("", "key")]
    [InlineData(" ", "key")]
    [InlineData("https://example.com", "")]
    [InlineData("https://example.com", " ")]
    public void AddAzureDocumentIntelligence_ThrowsOnMissingArguments(string endpoint, string apiKey)
    {
        var options = new LooseLeafOptions();
        Assert.Throws<ArgumentException>(() => options.AddAzureDocumentIntelligence(endpoint, apiKey));
    }

    [Fact]
    public void AddAzureDocumentIntelligence_ConfiguresOcrProviderFactory()
    {
        var options = new LooseLeafOptions()
            .AddAzureDocumentIntelligence("https://example.cognitiveservices.azure.com", "test-key");

        var provider = options.OcrProviderFactory(null!);
        Assert.IsType<AzureDocumentIntelligenceOcrProvider>(provider);
    }

    [Fact]
    public void AddLooseLeaf_ThrowsWhenNoStructuredExtractorConfigured()
    {
        var services = new ServiceCollection();

        Assert.Throws<InvalidOperationException>(() =>
            services.AddLooseLeaf(_ => { }));
    }

    [Fact]
    public void AddLooseLeaf_RegistersExtractor_WhenChatClientConfigured()
    {
        var services = new ServiceCollection();
        services.AddLooseLeaf(options =>
        {
            options.AddChatClient(new StubChatClient());
        });

        using var provider = services.BuildServiceProvider();
        var extractor = provider.GetService<ILooseLeafExtractor>();
        Assert.NotNull(extractor);
    }

    private sealed class StubChatClient : IChatCompletionClient
    {
        public Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default) =>
            Task.FromResult("{}");
    }
}
