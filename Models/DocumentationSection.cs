namespace ServiceManual.Models;

/// <summary>
/// Documentation section from CMS (documentation-section content type).
/// Used for the top-level documentation nav (tabs) and to group documentation pages.
/// </summary>
public class DocumentationSection
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool Enabled { get; set; }
    /// <summary>Pages in this section, ordered for side nav.</summary>
    public List<DocumentationPageSummary> Documentations { get; set; } = [];
}
