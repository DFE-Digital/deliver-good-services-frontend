using Microsoft.AspNetCore.Mvc;
using ServiceManual.Helpers;
using ServiceManual.Models;
using ServiceManual.Services;

namespace ServiceManual.Controllers;

[Route("standards/service-standards")]
[Route("standards/service-standard")]
public class ServiceStandardsController : Controller
{
    private readonly ICmsApiService _cmsApiService;
    private readonly ILogger<ServiceStandardsController> _logger;

    public ServiceStandardsController(ICmsApiService cmsApiService, ILogger<ServiceStandardsController> logger)
    {
        _cmsApiService = cmsApiService;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        try
        {
            var standards = await _cmsApiService.GetServiceStandardsAsync();
            return View("~/Views/Standards/ServiceStandards/Index.cshtml", standards);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading service standards index");
            ViewBag.ErrorMessage = "Unable to load service standards at this time.";
            return View("~/Views/Standards/ServiceStandards/Index.cshtml", new List<ServiceStandardSummary>());
        }
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Details(string slug, string? phase = null, string? role = null, string? strength = null)
    {
        try
        {
            var standard = await _cmsApiService.GetServiceStandardBySlugAsync(slug);
            if (standard is null)
                return NotFound();

            var allStandards = await _cmsApiService.GetServiceStandardsAsync();
            var ordered = allStandards
                .OrderBy(s => s.Point == 0 ? int.MaxValue : s.Point)
                .ThenBy(s => s.Title)
                .ToList();

            var index = ordered.FindIndex(s => s.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
            var previous = index > 0 ? ordered[index - 1] : null;
            var next = index >= 0 && index < ordered.Count - 1 ? ordered[index + 1] : null;

            var viewModel = new ServiceStandardPageViewModel
            {
                Standard = standard,
                Guidance = BuildGuidance(standard, phase, role, strength),
                AllStandards = ordered,
                CurrentSlug = slug,
                PreviousUrl = previous is null ? null : Url.Action("Details", "ServiceStandards", new { slug = previous.Slug }),
                PreviousLabel = previous is null ? null : BuildPointLabel(previous.Point, previous.Title),
                NextUrl = next is null ? null : Url.Action("Details", "ServiceStandards", new { slug = next.Slug }),
                NextLabel = next is null ? null : BuildPointLabel(next.Point, next.Title),
            };

            return View("~/Views/Standards/ServiceStandards/Details.cshtml", viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading service standard '{Slug}'", slug);
            ViewBag.ErrorMessage = "Unable to load this service standard at this time.";
            return View("~/Views/Standards/ServiceStandards/Details.cshtml", new ServiceStandardPageViewModel());
        }
    }

    private static ServicePointGuidanceViewModel BuildGuidance(ServiceStandardPage standard, string? phase, string? role, string? strength)
    {
        var allEntries = standard.Sections.SelectMany(s => s.ContentEntries).ToList();

        var phaseTabs = allEntries
            .SelectMany(e => e.Phases)
            .Where(p => !string.IsNullOrWhiteSpace(p.Slug) || !string.IsNullOrWhiteSpace(p.Title))
            .GroupBy(p => p.Slug, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(p => PhaseSortOrder(p.Slug))
            .ThenBy(p => p.Title)
            .ToList();

        var activePhase = phaseTabs
            .FirstOrDefault(p => p.Slug.Equals(phase, StringComparison.OrdinalIgnoreCase))
            ?? phaseTabs.FirstOrDefault();

        var filteredEntries = allEntries
            .Where(e => activePhase is null || e.Phases.Count == 0 || e.Phases.Any(p => p.Slug.Equals(activePhase.Slug, StringComparison.OrdinalIgnoreCase)))
            .Where(e => string.IsNullOrWhiteSpace(role) || e.Roles.Any(r => r.Slug.Equals(role, StringComparison.OrdinalIgnoreCase)))
            .Where(e => string.IsNullOrWhiteSpace(strength) || string.Equals(e.Strength, strength, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var roles = allEntries
            .SelectMany(e => e.Roles)
            .Where(r => !string.IsNullOrWhiteSpace(r.Slug) || !string.IsNullOrWhiteSpace(r.Title))
            .GroupBy(r => r.Slug, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(r => r.Title)
            .ToList();

        var strengths = allEntries
            .Select(e => e.Strength)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(StrengthSortOrder)
            .ThenBy(s => s)
            .Select(s => s!)
            .ToList();

        return new ServicePointGuidanceViewModel
        {
            IntroHtml = GovUkMarkdownHelper.ToGovUkHtmlForBody(standard.Body),
            ActivePhaseSlug = activePhase?.Slug ?? string.Empty,
            SelectedRoleSlug = string.IsNullOrWhiteSpace(role) ? null : role,
            SelectedStrength = string.IsNullOrWhiteSpace(strength) ? null : strength,
            PhaseTabs = phaseTabs,
            AvailableRoles = roles,
            AvailableStrengths = strengths,
            Entries = filteredEntries,
        };
    }

    private static int PhaseSortOrder(string? slug) => slug?.ToLowerInvariant() switch
    {
        "discovery" => 0,
        "alpha" => 1,
        "beta" => 2,
        "live" => 3,
        _ => 99,
    };

    private static int StrengthSortOrder(string? strength) => strength?.ToLowerInvariant() switch
    {
        "must" => 0,
        "should" => 1,
        "could" => 2,
        _ => 99,
    };

    private static string BuildPointLabel(int point, string title)
    {
        if (point > 0)
            return $"{point}. {title}";

        return title;
    }
}
