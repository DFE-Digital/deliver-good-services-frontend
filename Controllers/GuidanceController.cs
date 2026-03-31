using Microsoft.AspNetCore.Mvc;
using ServiceManual.Models;
using ServiceManual.Services;

namespace ServiceManual.Controllers;

public class GuidanceController : Controller
{
    private readonly ICmsApiService _cmsApiService;
    private readonly ISearchService _searchService;

    public GuidanceController(ICmsApiService cmsApiService, ISearchService searchService)
    {
        _cmsApiService = cmsApiService;
        _searchService = searchService;
    }

    [Route("guidance")]
    public async Task<IActionResult> Index([FromQuery] string? guidanceArea, [FromQuery] List<string>? professions, [FromQuery] string? search)
    {
        var page = await _cmsApiService.GetGuidanceIndexAsync();

        if (page is null)
            return NotFound();

        var selectedProfessionSlugs = (professions ?? [])
            .Where(profession => !string.IsNullOrWhiteSpace(profession))
            .Select(profession => profession.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var selectedProfessionSet = selectedProfessionSlugs.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var selectedAreaSlug = string.IsNullOrWhiteSpace(guidanceArea) ? null : guidanceArea.Trim();
        var selectedSearchTerm = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        var matchedGuidanceCardUrls = await GetMatchedGuidanceCardUrlsAsync(selectedSearchTerm);

        static IReadOnlyList<TagRef> ProfessionTagsForCollection(GuidanceAreaGroup area, GuidanceCollectionCard collection)
        {
            return collection.ApplicableProfessions
                .Concat(area.FeaturedProfessions)
                .Where(profession => !string.IsNullOrWhiteSpace(profession.Slug) || !string.IsNullOrWhiteSpace(profession.Title))
                .GroupBy(
                    profession => !string.IsNullOrWhiteSpace(profession.Slug)
                        ? profession.Slug
                        : profession.Title,
                    StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();
        }

        var visibleAreas = page.Areas
            .Where(area => string.IsNullOrWhiteSpace(selectedAreaSlug) || area.Slug.Equals(selectedAreaSlug, StringComparison.OrdinalIgnoreCase))
            .Select(area => new GuidanceAreaGroup
            {
                Name = area.Name,
                Slug = area.Slug,
                Summary = area.Summary,
                Description = area.Description,
                ColourHex = area.ColourHex,
                FeaturedProfessions = area.FeaturedProfessions,
                Collections = area.Collections
                    .Where(collection =>
                    {
                        var normalizedCollectionUrl = NormalizePath(collection.Url);
                        return
                        (selectedProfessionSet.Count == 0 ||
                        collection.ApplicableProfessions.Any(p => p.Title.Equals("All DDaT Professions", StringComparison.OrdinalIgnoreCase)) ||
                        ProfessionTagsForCollection(area, collection)
                            .Any(profession => selectedProfessionSet.Contains(profession.Slug)))
                        && (matchedGuidanceCardUrls == null
                            || (normalizedCollectionUrl != null && matchedGuidanceCardUrls.Contains(normalizedCollectionUrl)));
                    })
                    .ToList()
            })
            .Where(area => area.Collections.Count > 0)
            .ToList();

        var uniqueCards = page.Areas
            .SelectMany(area => area.Collections)
            .GroupBy(collection => collection.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

        var collectionProfessionMappings = page.Areas
            .SelectMany(area => area.Collections.Select(collection => new
            {
                CardKey = collection.Key,
                Professions = ProfessionTagsForCollection(area, collection)
            }))
            .Where(mapping => !string.IsNullOrWhiteSpace(mapping.CardKey))
            .SelectMany(mapping => mapping.Professions
                .Where(profession => !string.IsNullOrWhiteSpace(profession.Slug) && !string.IsNullOrWhiteSpace(profession.Title))
                .Select(profession => new
                {
                    mapping.CardKey,
                    Profession = profession
                }))
            .ToList();

        var professionFilters = collectionProfessionMappings
            .GroupBy(mapping => mapping.Profession.Slug, StringComparer.OrdinalIgnoreCase)
            .Select(group => new GuidanceFilterOption
            {
                Slug = group.First().Profession.Slug,
                Label = group.First().Profession.Title,
                Count = group
                    .Select(mapping => mapping.CardKey)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count()
            })
            .OrderBy(option => option.Label)
            .ToList();

        var model = new GuidanceIndexViewModel
        {
            Areas = visibleAreas,
            AreaFilters = page.Areas
                .Select(area => new GuidanceFilterOption
                {
                    Label = area.Name,
                    Slug = area.Slug,
                    Count = area.CollectionCount,
                    ColourHex = area.ColourHex
                })
                .ToList(),
            ProfessionFilters = professionFilters,
            SelectedGuidanceAreaSlug = selectedAreaSlug,
            SelectedProfessionSlugs = selectedProfessionSlugs,
            SelectedSearchTerm = selectedSearchTerm,
            TotalCollectionCount = uniqueCards.Count
        };

        ViewData["Title"] = "Guidance";
        ViewBag.HeroType = "guidance";
        ViewBag.HeroTitle = "Guidance";
        ViewBag.HeroIntro = "Browse guidance by area and filter collections by profession.";
        ViewBag.HeroBadgeText = "Guidance";

        return View("~/Views/Templates/Guidance.cshtml", model);
    }

    private async Task<HashSet<string>?> GetMatchedGuidanceCardUrlsAsync(string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return null;

        var types = new[] { "Collection", "Detailed Guide", "Detailed Guide Page", "Job description" };
        var results = await _searchService.SearchAsync(searchTerm, types);

        var matched = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var result in results)
        {
            AddGuidanceUrlMatch(matched, result.Url);
            AddGuidanceUrlMatch(matched, result.PartOfCollectionUrl);
        }

        return matched;
    }

    private static void AddGuidanceUrlMatch(ISet<string> matched, string? url)
    {
        var normalized = NormalizePath(url);
        if (normalized == null || !normalized.StartsWith("/guidance/", StringComparison.OrdinalIgnoreCase))
            return;

        matched.Add(normalized);

        // Guide page matches should reveal the parent guide card on the guidance index.
        var segments = normalized.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length >= 4
            && segments[0].Equals("guidance", StringComparison.OrdinalIgnoreCase)
            && segments[1].Equals("guides", StringComparison.OrdinalIgnoreCase))
        {
            matched.Add($"/guidance/guides/{segments[2]}");
        }
    }

    private static string? NormalizePath(string? pathOrUrl)
    {
        if (string.IsNullOrWhiteSpace(pathOrUrl))
            return null;

        if (Uri.TryCreate(pathOrUrl, UriKind.Absolute, out var absolute))
            return absolute.AbsolutePath.TrimEnd('/');

        var path = pathOrUrl.Trim();
        if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return null;

        if (!path.StartsWith('/'))
            path = "/" + path;

        var queryIndex = path.IndexOf('?');
        if (queryIndex >= 0)
            path = path[..queryIndex];

        var hashIndex = path.IndexOf('#');
        if (hashIndex >= 0)
            path = path[..hashIndex];

        var normalized = path.TrimEnd('/');
        return string.IsNullOrEmpty(normalized) ? "/" : normalized;
    }
}
