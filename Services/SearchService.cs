using ServiceManual.Models;

namespace ServiceManual.Services;

/// <summary>
/// Searches CMS content and DDT standards with priority scoring:
/// title match &gt; meta description/summary &gt; body (when available).
/// </summary>
public class SearchService : ISearchService
{
    private readonly ICmsApiService _cmsApiService;
    private readonly DdtStandardsApiService _standardsApiService;
    private readonly ILogger<SearchService> _logger;

    private const int ScoreTitleExact = 100;
    private const int ScoreTitleContains = 80;
    private const int ScoreMetaExact = 50;
    private const int ScoreMetaContains = 30;

    public SearchService(
        ICmsApiService cmsApiService,
        DdtStandardsApiService standardsApiService,
        ILogger<SearchService> logger)
    {
        _cmsApiService = cmsApiService;
        _standardsApiService = standardsApiService;
        _logger = logger;
    }

    public async Task<List<SearchResultItem>> SearchAsync(string? keywords, IReadOnlyList<string>? types = null)
    {
        var query = keywords?.Trim();
        if (string.IsNullOrEmpty(query))
            return [];

        var terms = query.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (terms.Length == 0)
            return [];

        var results = new List<SearchResultItem>();

        var includeCms = IncludeCms(types);
        var includeStandards = IncludeStandards(types);

        if (includeCms)
        {
            var cmsItems = await _cmsApiService.GetAllPublishedContentAsync();
            foreach (var item in cmsItems)
            {
                var score = ScoreContentItem(item.Title, item.MetaDescription, null, terms);
                if (score > 0)
                {
                    results.Add(new SearchResultItem
                    {
                        Title = item.Title,
                        Summary = item.MetaDescription,
                        Url = item.Url,
                        ContentType = item.ContentType,
                        Source = "Cms",
                        Score = score,
                        PartOfCollectionTitle = item.CollectionTitle,
                        PartOfCollectionUrl = !string.IsNullOrEmpty(item.CollectionSlug) ? $"/guidance/collections/{item.CollectionSlug}" : null
                    });
                }
            }
        }

        if (includeStandards)
        {
            try
            {
                var standardsResponse = await _standardsApiService.GetPublishedStandardsAsync(
                    search: query, page: 1, pageSize: 100);
                var standards = standardsResponse?.Data ?? [];
                foreach (var s in standards)
                {
                    var score = ScoreContentItem(s.Title, s.Summary, null, terms);
                    if (score > 0)
                    {
                        var slug = s.Slug ?? s.StandardUuid ?? s.Id.ToString();
                        results.Add(new SearchResultItem
                        {
                            Title = s.Title ?? string.Empty,
                            Summary = s.Summary,
                            Url = $"/standards/ddt-standards/{slug}",
                            ContentType = "Standard",
                            Source = "Standard",
                            Score = score
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Standards search failed for query '{Query}'", query);
            }
        }

        var filtered = types != null && types.Count > 0
            ? results.Where(r => types.Any(t => string.Equals(r.ContentType, t, StringComparison.OrdinalIgnoreCase))).ToList()
            : results;

        return filtered
            .OrderByDescending(r => r.Score)
            .ThenBy(r => r.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool IncludeCms(IReadOnlyList<string>? types)
    {
        if (types == null || types.Count == 0) return true;
        var cmsTypes = new[] { "Article", "Collection", "Detailed Guide", "Detailed Guide Page", "HTML Page", "Roadmap", "Lifecycle", "Lifecycle Stage" };
        return types.Any(t => cmsTypes.Contains(t, StringComparer.OrdinalIgnoreCase));
    }

    private static bool IncludeStandards(IReadOnlyList<string>? types)
    {
        if (types == null || types.Count == 0) return true;
        return types.Any(t => t.Equals("Standard", StringComparison.OrdinalIgnoreCase));
    }

    private static int ScoreContentItem(string? title, string? meta, string? body, string[] terms)
    {
        if (terms.Length == 0) return 0;
        var titleNorm = Normalize(title);
        var metaNorm = Normalize(meta);
        var bodyNorm = Normalize(body);
        if (string.IsNullOrEmpty(titleNorm) && string.IsNullOrEmpty(metaNorm) && string.IsNullOrEmpty(bodyNorm))
            return 0;

        var score = 0;
        var queryNorm = string.Join(" ", terms);

        foreach (var term in terms)
        {
            if (string.IsNullOrEmpty(term)) continue;

            if (titleNorm.Contains(term, StringComparison.OrdinalIgnoreCase))
                score += titleNorm.Equals(term, StringComparison.OrdinalIgnoreCase) ? ScoreTitleExact : ScoreTitleContains;
            else if (!string.IsNullOrEmpty(metaNorm) && metaNorm.Contains(term, StringComparison.OrdinalIgnoreCase))
                score += metaNorm.Equals(term, StringComparison.OrdinalIgnoreCase) ? ScoreMetaExact : ScoreMetaContains;
            else if (!string.IsNullOrEmpty(bodyNorm) && bodyNorm.Contains(term, StringComparison.OrdinalIgnoreCase))
                score += 10;
        }

        if (titleNorm.Contains(queryNorm, StringComparison.OrdinalIgnoreCase))
            score += 20;
        if (!string.IsNullOrEmpty(metaNorm) && metaNorm.Contains(queryNorm, StringComparison.OrdinalIgnoreCase))
            score += 15;

        return score;
    }

    private static string? Normalize(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
