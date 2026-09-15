using LooseLeaf.Abstractions;
using LooseLeaf.Ocr;
using LooseLeaf.Models;
using LooseLeaf.Core;

namespace LooseLeaf.Configuration;

public sealed class LooseLeafOptions
{
    internal Func<IServiceProvider, IOcrProvider?> OcrProviderFactory { get; private set; } = _ => null;

    internal Func<IServiceProvider, IStructuredDataExtractor>? StructuredDataExtractorFactory { get; private set; }

    public OcrMode OcrMode { get; set; } = OcrMode.Auto;

    public int MinimumNativeTextCharacters { get; set; } = 40;

    public TimeSpan OcrTimeout { get; set; } = TimeSpan.FromMinutes(2);

    public TimeSpan OcrPollingInterval { get; set; } = TimeSpan.FromSeconds(1);

    public LooseLeafOptions AddOcrProvider(IOcrProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        OcrProviderFactory = _ => provider;
        return this;
    }

    public LooseLeafOptions AddStructuredDataExtractor(IStructuredDataExtractor extractor)
    {
        ArgumentNullException.ThrowIfNull(extractor);
        StructuredDataExtractorFactory = _ => extractor;
        return this;
    }

    public LooseLeafOptions AddChatClient(IChatCompletionClient chatClient)
    {
        ArgumentNullException.ThrowIfNull(chatClient);
        StructuredDataExtractorFactory = _ => new ChatPromptStructuredDataExtractor(chatClient);
        return this;
    }

    public LooseLeafOptions AddAzureDocumentIntelligence(string endpoint, string apiKey)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new ArgumentException("Azure endpoint is required.", nameof(endpoint));
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("Azure API key is required.", nameof(apiKey));
        }

        OcrProviderFactory = _ => new AzureDocumentIntelligenceOcrProvider(
            endpoint,
            apiKey,
            timeout: OcrTimeout,
            pollingInterval: OcrPollingInterval);
        return this;
    }
}
