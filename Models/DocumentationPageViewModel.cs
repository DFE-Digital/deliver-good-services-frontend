namespace ServiceManual.Models;

/// <summary>
/// View model for rendering a single documentation page (CMS body as HTML).
/// </summary>
public class DocumentationPageViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? MetaDescription { get; set; }
    public string BodyHtml { get; set; } = string.Empty;
    public string? LastReviewedDateDisplay { get; set; }
    public string SectionSlug { get; set; } = string.Empty;
    public string SectionTitle { get; set; } = string.Empty;
}
