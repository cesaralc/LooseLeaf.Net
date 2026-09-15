using System.Text;
using System.Text.Json;
using LooseLeaf.Abstractions;
using LooseLeaf.Models;

namespace LooseLeaf.Core;

public sealed class ChatPromptStructuredDataExtractor : IStructuredDataExtractor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IChatCompletionClient _chatClient;

    public ChatPromptStructuredDataExtractor(IChatCompletionClient chatClient)
    {
        _chatClient = chatClient;
    }

    public async Task<StructuredExtractionResult<T>> ExtractAsync<T>(
        ProcessedDocument document,
        CancellationToken cancellationToken = default)
    {
        var prompt = BuildPrompt<T>(document);
        var response = await _chatClient.CompleteAsync(prompt, cancellationToken);

        var payload = JsonSerializer.Deserialize<ExtractionPayload<T>>(response, JsonOptions)
            ?? throw new InvalidOperationException("The LLM returned an empty extraction payload.");

        if (payload.Data is null)
        {
            throw new InvalidOperationException("The LLM payload did not contain data.");
        }

        return new StructuredExtractionResult<T>
        {
            Data = payload.Data,
            Sources = payload.Sources ??
                new Dictionary<string, FieldSource>(StringComparer.OrdinalIgnoreCase)
        };
    }

    private static string BuildPrompt<T>(ProcessedDocument document)
    {
        var schema = typeof(T).FullName ?? typeof(T).Name;
        var docJson = JsonSerializer.Serialize(document);
        var sb = new StringBuilder();
        sb.AppendLine("Extract structured JSON data from this document.");
        sb.AppendLine($"Target model type: {schema}");
        sb.AppendLine("Return JSON only with this exact shape:");
        sb.AppendLine("{");
        sb.AppendLine("  \"data\": { ... model fields ... },");
        sb.AppendLine("  \"sources\": {");
        sb.AppendLine("    \"Property.Path\": {");
        sb.AppendLine("      \"page\": 1,");
        sb.AppendLine("      \"text\": \"original source text\",");
        sb.AppendLine("      \"bounds\": { \"x\": 0.1, \"y\": 0.2, \"width\": 0.3, \"height\": 0.4 },");
        sb.AppendLine("      \"ocrConfidence\": 0.99,");
        sb.AppendLine("      \"extractionConfidence\": 0.95");
        sb.AppendLine("    }");
        sb.AppendLine("  }");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("Document JSON:");
        sb.AppendLine(docJson);
        return sb.ToString();
    }

    private sealed class ExtractionPayload<TData>
    {
        public TData? Data { get; init; }

        public Dictionary<string, FieldSource>? Sources { get; init; }
    }
}
