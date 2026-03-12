namespace ServiceManual.Models;

/// <summary>
/// View model for rendering a single documentation page (CMS body as HTML).
/// Mirrors guide-style: contents list, body, pagination.
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
    /// <summary>Contents nav items (Overview + pages in section).</summary>
    public List<GuideContentsItem> ContentsItems { get; set; } = [];
    public string? PaginationPrevUrl { get; set; }
    public string? PaginationPrevLabel { get; set; }
    public string? PaginationNextUrl { get; set; }
    public string? PaginationNextLabel { get; set; }
}
