namespace LooseLeaf.Models;

public sealed class ValidationIssue
{
    public string Field { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;
}
