using ServiceManual.Models;

namespace ServiceManual.Services;

public interface ISearchService
{
    /// <summary>
    /// Search CMS content and DDT standards by keywords.
    /// Results are scored by priority: title match &gt; meta description/summary &gt; body (when available).
    /// </summary>
    /// <param name="keywords">Search query (optional; empty returns no results).</param>
    /// <param name="types">Optional filter by content type or source (e.g. "Standard", "Single Page Guide").</param>
    Task<List<SearchResultItem>> SearchAsync(string? keywords, IReadOnlyList<string>? types = null);
}
