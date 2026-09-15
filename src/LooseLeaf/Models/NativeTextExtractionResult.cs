namespace LooseLeaf.Models;

public sealed record NativeTextExtractionResult(
    ProcessedDocument Document,
    int ExtractedCharacterCount);
