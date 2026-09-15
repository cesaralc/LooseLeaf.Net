using LooseLeaf.Abstractions;
using LooseLeaf.Configuration;
using LooseLeaf.Core;
using Microsoft.Extensions.DependencyInjection;

namespace LooseLeaf;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLooseLeaf(
        this IServiceCollection services,
        Action<LooseLeafOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new LooseLeafOptions();
        configure(options);

        if (options.StructuredDataExtractorFactory is null)
        {
            throw new InvalidOperationException(
                "No chat/data extractor is configured. Call options.AddChatClient(...) or options.AddStructuredDataExtractor(...).");
        }

        services.AddSingleton(options);
        services.AddSingleton<INativeTextExtractor, PdfNativeTextExtractor>();
        services.AddSingleton(sp => options.StructuredDataExtractorFactory(sp));
        services.AddSingleton<IEnumerable<IOcrProvider>>(sp =>
        {
            var provider = options.OcrProviderFactory(sp);
            return provider is null ? Array.Empty<IOcrProvider>() : [provider];
        });
        services.AddSingleton<ILooseLeafExtractor, Core.LooseLeafExtractor>();

        return services;
    }
}
