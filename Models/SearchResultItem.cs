namespace ServiceManual.Models;

/// <summary>
/// A single search result from either CMS content or DDT Standards.
/// </summary>
public class SearchResultItem
{
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string Url { get; set; } = string.Empty;
    /// <summary>Display label for the content type (e.g. "Single Page Guide", "Standard").</summary>
    public string ContentType { get; set; } = string.Empty;
    /// <summary>Source: "Cms" or "Standard". Used for filtering.</summary>
    public string Source { get; set; } = string.Empty;
    /// <summary>Relevance score for ordering (higher = better match).</summary>
    public int Score { get; set; }
    /// <summary>When content is part of a collection, the collection title (for "Part of collection" link).</summary>
    public string? PartOfCollectionTitle { get; set; }
    /// <summary>When content is part of a collection, the collection URL.</summary>
    public string? PartOfCollectionUrl { get; set; }
}
