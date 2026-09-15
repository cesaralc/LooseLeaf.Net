namespace LooseLeaf.Abstractions;

public interface IChatCompletionClient
{
    Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default);
}
