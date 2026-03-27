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
    public async Task<IActionResult> Index([FromQuery] string? guidanceArea, [FromQuery] List<string>? professions)
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

        static IReadOnlyList<TagRef> ProfessionTagsForCollection(GuidanceAreaGroup area, GuidanceCollectionCard collection)
        {
            return collection.ApplicableProfessions.Count > 0
                ? collection.ApplicableProfessions
                : area.FeaturedProfessions;
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
                        selectedProfessionSet.Count == 0 ||
                        ProfessionTagsForCollection(area, collection)
                            .Any(profession => selectedProfessionSet.Contains(profession.Slug)))
                    .ToList()
            })
            .Where(area => area.Collections.Count > 0)
            .ToList();

        var uniqueCollections = page.Areas
            .SelectMany(area => area.Collections)
            .GroupBy(collection => collection.Slug, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

        var professionFilters = uniqueCollections
            .SelectMany(collection =>
                page.Areas
                    .Where(area => area.Collections.Any(areaCollection => areaCollection.Slug.Equals(collection.Slug, StringComparison.OrdinalIgnoreCase)))
                    .SelectMany(area => ProfessionTagsForCollection(area, collection)))
            .Where(profession => !string.IsNullOrWhiteSpace(profession.Slug) && !string.IsNullOrWhiteSpace(profession.Title))
            .GroupBy(profession => profession.Slug, StringComparer.OrdinalIgnoreCase)
            .Select(group => new GuidanceFilterOption
            {
                Slug = group.First().Slug,
                Label = group.First().Title,
                Count = uniqueCollections.Count(collection =>
                    page.Areas
                        .Where(area => area.Collections.Any(areaCollection => areaCollection.Slug.Equals(collection.Slug, StringComparison.OrdinalIgnoreCase)))
                        .SelectMany(area => ProfessionTagsForCollection(area, collection))
                        .Any(profession => profession.Slug.Equals(group.Key, StringComparison.OrdinalIgnoreCase)))
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
