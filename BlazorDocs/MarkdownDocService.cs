using System.Text.RegularExpressions;
using Markdig;
using Microsoft.Extensions.Options;

namespace BlazorDocs;

public partial class MarkdownDocService : IMarkdownDocService
{
    private readonly IOptions<MarkdownDocOptions> options;

    // Lookup from slug (always lower) => DocItem
    private Dictionary<string, DocItem> _docLookup = new();
    
    // The root of our doc tree (folder structure + docs)
    private DocNode? _rootNode;

    private static readonly MarkdownPipeline MarkdownPipeline =
        new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

    public MarkdownDocService(IOptions<MarkdownDocOptions> options)
    {
        this.options = options;
        BuildDocTree();
    }

    // todo: could be better to do this lazily on 1st request instead of ctor
    private void BuildDocTree()
    {
        _docLookup = new();
        _rootNode = new DocNode
        {
            Segment = "root",
            Position = 0
        };

        var docsFolder = Path.Combine(options.Value.ContentRootPath, "Docs");
        if (Directory.Exists(docsFolder))
        {
            var node = LoadDocsRecursive(docsFolder, parentSlug: "", isRoot: true);
            _rootNode.Children.AddRange(node.Children);
        }

        // Sort the entire tree after building
        SortNode(_rootNode);
    }
    
    public Task<DocNode> GetDocTreeAsync()
    {
        return Task.FromResult(_rootNode ?? new DocNode { Segment = "root" });
    }

    public string? MapSlugToPath(string slug)
    {
        // Convert inbound slug to lower
        slug = slug.ToLower();
        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = "index";
        }

        return _docLookup.TryGetValue(slug, out var doc) ? doc.FilePath : null;
    }

    public string GetHtmlContent(string? docPath)
    {
        if (docPath == null || !File.Exists(docPath))
            return "<p>Not Found</p>";

        var doc = _docLookup.Values.FirstOrDefault(d =>
            d.FilePath.Equals(docPath, StringComparison.OrdinalIgnoreCase));

        return doc?.HtmlContent ?? "<p>Not Found</p>";
    }

    public Task<List<DocItem>> GetAllDocsAsync()
    {
        return Task.FromResult(_docLookup.Values.ToList());
    }

    public Task<DocItem?> GetDocBySlugAsync(string? slug)
    {
        // Convert inbound slug to lower
        slug = slug?.ToLower();

        // If empty slug => try "index"
        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = "index";

            // If there's no actual index.md
            // and user has opted in to fallback to first doc at root
            if (!_docLookup.ContainsKey(slug) && options.Value.UseImplicitFirstDocAsRoot)
            {
                // Because _rootNode is the root-level node,
                // and we sorted it in SortNode(_rootNode),
                // its Children are in ascending order by Position.
                // So you can either pick Position == 1 specifically,
                // or pick the first doc child.
            
                // Example 1: specifically look for position == 1
                var firstDocNode = _rootNode?.Children
                    .FirstOrDefault(c => c is { Position: 1, Doc: not null });

                // -- OR, if you simply want the earliest doc, skip the .Where() on Position:
                // var firstDocNode = _rootNode?.Children
                //     .FirstOrDefault(c => c.Doc != null);

                if (firstDocNode?.Doc != null)
                {
                    slug = firstDocNode.Doc.Slug; 
                }
            }
        }

        // Standard doc lookup
        var result = _docLookup.GetValueOrDefault(slug);
        
        return Task.FromResult(result);
    }

    /// <summary>
    /// Recursively scans `folderPath`, building DocNodes for folders/files.
    /// If `isRoot` is true AND `ExcludeRootFolderSegment` is true, we skip the folder name in the slug.
    /// Otherwise, we incorporate the folder name (in lowercase).
    /// </summary>
    private DocNode LoadDocsRecursive(string folderPath, string parentSlug, bool isRoot)
    {
        var resultNode = new DocNode();

        // e.g. "01-Configuration"
        var folderName = Path.GetFileName(folderPath);
        var (folderPosition, folderSlugSegment, folderRawName) 
            = ExtractPositionSlugAndTitle_Folder(folderName);

        // Combine with parent slug, but if it's the root level and we're excluding, skip it
        string currentFolderSlug;
        if (isRoot && options.Value.ExcludeRootFolderSegment)
        {
            // Skip top-level folder name
            currentFolderSlug = parentSlug; // usually ""
            resultNode.Segment = "root";
            resultNode.Position = 0;
        }
        else
        {
            // Normal logic
            // Convert folderSlugSegment to lower
            folderSlugSegment = folderSlugSegment.ToLower();

            currentFolderSlug = string.IsNullOrEmpty(parentSlug)
                ? folderSlugSegment
                : $"{parentSlug}/{folderSlugSegment}";

            currentFolderSlug = currentFolderSlug.ToLower();
            
            resultNode.Segment = folderSlugSegment; // For display or sorting
            resultNode.Position = folderPosition;
        }

        // Check for index.md => doc for the folder
        var indexFilePath = Path.Combine(folderPath, "index.md");
        if (File.Exists(indexFilePath))
        {
            var docItem = CreateDocItem(indexFilePath, currentFolderSlug, resultNode.Position, folderRawName);
            resultNode.Doc = docItem;
            // Add to lookup using docItem.Slug (already lower from CreateDocItem)
            _docLookup[docItem.Slug] = docItem;
        }
        else
        {
            resultNode.Doc = null;
        }

        // Other .md files
        var allMdFiles = Directory.GetFiles(folderPath, "*.md", SearchOption.TopDirectoryOnly)
            .Where(f => !Path.GetFileName(f).Equals("index.md", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var filePath in allMdFiles)
        {
            var fileNameNoExt = Path.GetFileNameWithoutExtension(filePath);
            var (filePos, fileSlugSegment, fileRawTitle) 
                = ExtractPositionSlugAndTitle_File(fileNameNoExt);

            // Convert the fileSlugSegment to lower
            fileSlugSegment = fileSlugSegment.ToLower();

            // Combine with folder slug
            var docSlug = string.IsNullOrEmpty(currentFolderSlug)
                ? fileSlugSegment
                : $"{currentFolderSlug}/{fileSlugSegment}";

            docSlug = docSlug.ToLower();

            var docItem = CreateDocItem(filePath, docSlug, filePos, fileRawTitle);
            _docLookup[docItem.Slug] = docItem;

            var childNode = new DocNode
            {
                Segment = fileSlugSegment, // display
                Position = filePos,
                Doc = docItem
            };
            resultNode.Children.Add(childNode);
        }

        // Subfolders
        var subfolders = Directory.GetDirectories(folderPath);
        foreach (var subfolder in subfolders)
        {
            if (subfolder.EndsWith(".git") || subfolder.EndsWith(".idea"))
                continue;

            var childNode = LoadDocsRecursive(subfolder, currentFolderSlug, false);
            
            // If the child node has content
            if (childNode.Segment != "root" || childNode.Children.Any() || childNode.Doc != null)
            {
                resultNode.Children.Add(childNode);
            }
        }

        return resultNode;
    }

    private DocItem CreateDocItem(string filePath, string docSlug, int position, string fallbackTitle)
    {
        var markdownText = File.ReadAllText(filePath);
        var htmlContent = Markdown.ToHtml(markdownText, MarkdownPipeline);

        var headingTitle = ExtractTitleFromMarkdown(markdownText);
        var finalTitle = string.IsNullOrEmpty(headingTitle) ? fallbackTitle : headingTitle;

        // Ensure docSlug is lower
        docSlug = docSlug.ToLower();

        return new DocItem
        {
            FilePath = filePath,
            Slug = docSlug,
            Position = position,
            Title = finalTitle,
            HtmlContent = htmlContent
        };
    }

    private void SortNode(DocNode node)
    {
        node.Children = node.Children
            .OrderBy(n => n.Position)
            .ThenBy(n => n.Segment, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var child in node.Children)
        {
            SortNode(child);
        }
    }

    /// <summary>
    /// Folder: remove numeric prefix, replace spaces with dashes (uppercase left alone, then we ToLower() later).
    /// e.g. "02-Databases" => (2, "Databases", "Databases")
    /// We'll do final .ToLower() when combining parent + child.
    /// </summary>
    private (int position, string slug, string rawName) ExtractPositionSlugAndTitle_Folder(string folderName)
    {
        var match = NodePositionAndTitleRegex().Match(folderName);
        if (match.Success)
        {
            if (int.TryParse(match.Groups["pos"].Value, out var pos))
            {
                var restString = match.Groups["rest"].Value.Trim();
                var slugVersion = restString.Replace(" ", "-"); // uppercase not changed here
                return (pos, slugVersion, restString);
            }
        }

        var fallbackSlug = folderName.Replace(" ", "-");
        return (999, fallbackSlug, folderName);
    }

    /// <summary>
    /// Files: parse numeric prefix, then spaces->dashes, forced lower if found.
    /// e.g. "05 - Setup Guide" => (5, "setup-guide", "Setup Guide")
    /// We do final .ToLower() outside in LoadDocsRecursive for consistency.
    /// </summary>
    private (int position, string slug, string rawTitle) ExtractPositionSlugAndTitle_File(string fileNameNoExt)
    {
        var match = NodePositionAndTitleRegex().Match(fileNameNoExt);
        if (match.Success)
        {
            var posString = match.Groups["pos"].Value;
            var restString = match.Groups["rest"].Value.Trim();
            if (int.TryParse(posString, out var pos))
            {
                // spaces->dashes, not forcing lower at this step
                var slugVersion = restString.Replace(" ", "-"); 
                return (pos, slugVersion, restString);
            }
        }

        // No numeric prefix
        var fallbackSlug = fileNameNoExt.Replace(" ", "-");
        return (999, fallbackSlug, fileNameNoExt);
    }

    private string? ExtractTitleFromMarkdown(string markdown)
    {
        using var reader = new StringReader(markdown);
        while (reader.ReadLine() is { } line)
        {
            if (line.StartsWith("# "))
            {
                return line[2..].Trim();
            }
        }
        return null;
    }

    [GeneratedRegex(@"^(?<pos>\d+)[- ]*(?<rest>.*)$")]
    private static partial Regex NodePositionAndTitleRegex();
}