using Microsoft.Extensions.DependencyInjection;

namespace BlazorDocs;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMarkdownDocs(this IServiceCollection services, string docsRoot)
    {
        services.AddOptions().Configure<MarkdownDocOptions>(c => c.ContentRootPath = docsRoot);
        services.AddScoped<IMarkdownDocService, MarkdownDocService>();
        return services;
    }
}

public class DocItem
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;

    // New property for numeric ordering
    public int Position { get; set; } = 999;
}