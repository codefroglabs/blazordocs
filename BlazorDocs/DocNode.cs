namespace BlazorDocs;

public class DocNode
{
    /// <summary>
    /// The segment name for this node (e.g. "Databases", "mysql").
    /// </summary>
    public string Segment { get; set; } = string.Empty;

    /// <summary>
    /// If this node corresponds to a doc (like index.md or a normal doc),
    /// store it here. This might be null if there's no direct doc (folder only).
    /// </summary>
    public DocItem? Doc { get; set; }

    /// <summary>
    /// Child nodes (subfolders or docs).
    /// </summary>
    public List<DocNode> Children { get; set; } = new();

    /// <summary>
    /// For sorting. Typically from the doc's Position, if the doc is an index
    /// or the first doc in that node. Or you can store a separate field.
    /// </summary>
    public int Position { get; set; } = 999;
}