using Microsoft.AspNetCore.Mvc;
using ServiceManual.Models;
using ServiceManual.Services;

namespace ServiceManual.Controllers;

public class GuidanceController : Controller
{
    private readonly ICmsApiService _cmsApiService;

    public GuidanceController(ICmsApiService cmsApiService)
    {
        _cmsApiService = cmsApiService;
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

        static bool MatchesSearch(GuidanceAreaGroup area, GuidanceCollectionCard collection, string? searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return true;

            var term = searchTerm.Trim();
            var texts = new[]
            {
                area.Name,
                area.Summary,
                area.Description,
                collection.Title,
                collection.Description,
                string.Join(" ", collection.Tags),
                string.Join(" ", collection.ApplicableProfessions.Select(profession => profession.Title)),
                string.Join(" ", collection.AlsoInAreas.Select(areaRef => areaRef.Title))
            };

            return texts.Any(text => !string.IsNullOrWhiteSpace(text)
                && text.Contains(term, StringComparison.OrdinalIgnoreCase));
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
                        (selectedProfessionSet.Count == 0 ||
                        ProfessionTagsForCollection(area, collection)
                            .Any(profession => selectedProfessionSet.Contains(profession.Slug)))
                        && MatchesSearch(area, collection, selectedSearchTerm))
                    .ToList()
            })
            .Where(area => area.Collections.Count > 0)
            .ToList();

        var uniqueCollections = page.Areas
            .SelectMany(area => area.Collections)
            .GroupBy(collection => collection.Slug, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

        var collectionProfessionMappings = page.Areas
            .SelectMany(area => area.Collections.Select(collection => new
            {
                CollectionSlug = collection.Slug,
                Professions = ProfessionTagsForCollection(area, collection)
            }))
            .Where(mapping => !string.IsNullOrWhiteSpace(mapping.CollectionSlug))
            .SelectMany(mapping => mapping.Professions
                .Where(profession => !string.IsNullOrWhiteSpace(profession.Slug) && !string.IsNullOrWhiteSpace(profession.Title))
                .Select(profession => new
                {
                    mapping.CollectionSlug,
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
                    .Select(mapping => mapping.CollectionSlug)
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
            TotalCollectionCount = uniqueCollections.Count
        };

        ViewData["Title"] = "Guidance";
        ViewBag.HeroType = "guidance";
        ViewBag.HeroTitle = "Guidance";
        ViewBag.HeroIntro = "Browse guidance by area and filter collections by profession.";
        ViewBag.HeroBadgeText = "Guidance";

        return View("~/Views/Templates/Guidance.cshtml", model);
    }
}
