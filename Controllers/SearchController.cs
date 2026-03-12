using Microsoft.AspNetCore.Mvc;
using ServiceManual.Models;
using ServiceManual.Services;

namespace ServiceManual.Controllers;

[Route("search")]
public class SearchController : Controller
{
    private readonly ISearchService _searchService;
    private readonly ILogger<SearchController> _logger;

    public static readonly IReadOnlyList<string> AllContentTypes =
    [
        "Collection",
        "Detailed Guide",
        "Detailed Guide Page",
        "HTML Page",
        "Lifecycle",
        "Lifecycle Stage",
        "Standard"
    ];

    public SearchController(ISearchService searchService, ILogger<SearchController> logger)
    {
        _searchService = searchService;
        _logger = logger;
    }

    /// <summary>
    /// Search results page. GET /search/all?keywords=...&type=...&type=...&highlight=1
    /// </summary>
    [HttpGet("all")]
    [HttpGet("")]
    public async Task<IActionResult> Index(string? keywords, [FromQuery(Name = "type")] List<string>? types = null, [FromQuery] string? highlight = null)
    {
        var query = keywords?.Trim();
        var typeFilter = types?.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().ToList();
        var showHighlight = string.Equals(highlight, "1", StringComparison.OrdinalIgnoreCase) || string.Equals(highlight, "true", StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrEmpty(query))
        {
            return View(new SearchViewModel
            {
                Keywords = "",
                Types = typeFilter ?? [],
                Results = [],
                Facets = [],
                HighlightKeywords = false
            });
        }

        var results = await _searchService.SearchAsync(query, typeFilter);

        // Faceted search: derive facets from current result set (content type -> count)
        var facets = results
            .GroupBy(r => r.ContentType, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key)
            .Select(g => new SearchFacet { ContentType = g.Key, Count = g.Count() })
            .ToList();

        return View(new SearchViewModel
        {
            Keywords = query,
            Types = typeFilter ?? [],
            Results = results,
            Facets = facets,
            HighlightKeywords = showHighlight
        });
    }
}

public class SearchViewModel
{
    public string Keywords { get; set; } = string.Empty;
    public List<string> Types { get; set; } = [];
    public List<SearchResultItem> Results { get; set; } = [];
    /// <summary>Content type facets with counts from the current result set.</summary>
    public List<SearchFacet> Facets { get; set; } = [];
    /// <summary>When true, wrap matching keywords in results with &lt;mark&gt; for highlighting.</summary>
    public bool HighlightKeywords { get; set; }
}
