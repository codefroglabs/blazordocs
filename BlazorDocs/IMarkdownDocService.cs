namespace BlazorDocs;

public interface IMarkdownDocService
{
    Task<List<DocItem>> GetAllDocsAsync();
    Task<DocItem?> GetDocBySlugAsync(string? slug);
    string? GetHtmlContent(string docPath);

    string? MapSlugToPath(string slug);

    Task<DocNode> GetDocTreeAsync();
}