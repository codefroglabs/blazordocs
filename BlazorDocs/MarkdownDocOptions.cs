namespace BlazorDocs;

public record MarkdownDocOptions
{
    /// <summary>
    /// If true, omit the top-level folder from the slug
    /// </summary>
    public bool ExcludeRootFolderSegment { get; set; } = true;
    
    /// <summary>
    /// Typically IWebHostEnvironment.ContentRootPath
    /// </summary>
    public required string ContentRootPath { get; set; }
    
    /// <summary>
    /// Uses the first doc as the root node in the tree. If false, the root node is the `index.md` file.
    /// </summary>
    public bool UseImplicitFirstDocAsRoot { get; set; } = true;
}