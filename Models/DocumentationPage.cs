namespace ServiceManual.Models;

/// <summary>
/// Full documentation page from CMS (documentation content type).
/// </summary>
public class DocumentationPage
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? MetaDescription { get; set; }
    /// <summary>Richtext/markdown body from CMS.</summary>
    public string? Body { get; set; }
    public string? LastReviewedDateDisplay { get; set; }
    public string SectionSlug { get; set; } = string.Empty;
    public string SectionTitle { get; set; } = string.Empty;
}
