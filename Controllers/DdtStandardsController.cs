using Microsoft.AspNetCore.Mvc;
using ServiceManual.Services;

namespace ServiceManual.Controllers;

[Route("standards/ddt-standards")]
[Route("standards/ddtstandards")] // legacy path (no hyphen)
public class DdtStandardsController : Controller
{
    private readonly DdtStandardsApiService _apiService;
    private readonly ILogger<DdtStandardsController> _logger;

    public DdtStandardsController(DdtStandardsApiService apiService, ILogger<DdtStandardsController> logger)
    {
        _apiService = apiService;
        _logger = logger;
    }

    /// <summary>
    /// List published DDT standards (filterable, sortable).
    /// </summary>
    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, string[]? category, string? sortBy, string? sortDirection, int page = 1)
    {
        try
        {
            var categoriesList = category?.Where(c => !string.IsNullOrWhiteSpace(c)).ToList() ?? new List<string>();

            var response = await _apiService.GetPublishedStandardsAsync(
                search: search,
                categories: categoriesList,
                sortBy: sortBy,
                sortDirection: sortDirection,
                page: page,
                pageSize: 10);

            if (response == null)
            {
                ViewBag.ErrorMessage = "Unable to load standards at this time.";
                return View("~/Views/Standards/DdtStandards/Index.cshtml", new DdtStandardsResponse { Data = [] });
            }

            ViewBag.Search = search;
            ViewBag.Category = categoriesList;
            ViewBag.SortBy = sortBy;
            ViewBag.SortDirection = sortDirection;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = response.Pagination?.TotalPages ?? 1;
            ViewBag.TotalRecords = response.Pagination?.TotalRecords ?? 0;

            // Always show all categories in the filter: when filters are applied, get categories from an unfiltered request
            var dataForCategories = response.Data;
            if (!string.IsNullOrWhiteSpace(search) || categoriesList.Count > 0)
            {
                var unfiltered = await _apiService.GetPublishedStandardsAsync(page: 1, pageSize: 500);
                if (unfiltered?.Data != null && unfiltered.Data.Count > 0)
                    dataForCategories = unfiltered.Data;
            }
            var allCategories = dataForCategories
                .SelectMany(s => s.Categories ?? [])
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct()
                .OrderBy(c => c!)
                .ToList();

            ViewBag.Categories = allCategories;

            response.Data = response.Data.OrderBy(s => s.Title).ToList();

            return View("~/Views/Standards/DdtStandards/Index.cshtml", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading DDT standards");
            ViewBag.ErrorMessage = "An error occurred while loading standards. Please try again later.";
            return View("~/Views/Standards/DdtStandards/Index.cshtml", new DdtStandardsResponse { Data = [] });
        }
    }

    /// <summary>
    /// Show a single DDT standard by slug.
    /// </summary>
    [HttpGet("{slug}")]
    public async Task<IActionResult> Details(string slug)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                ViewBag.ErrorMessage = "Standard slug is required.";
                return View("~/Views/Standards/DdtStandards/Details.cshtml", (DdtStandardDetailDto?)null);
            }

            var standard = await _apiService.GetStandardBySlugAsync(slug);

            if (standard == null)
            {
                ViewBag.ErrorMessage = "Standard not found. The standard may not exist or may not be published.";
                return View("~/Views/Standards/DdtStandards/Details.cshtml", (DdtStandardDetailDto?)null);
            }

            var compassBaseUrl = _apiService.GetCompassBaseUrl();
            ViewBag.CompassStandardUrl = $"{compassBaseUrl.TrimEnd('/')}/DdtStandards/Details/{standard.Id}";

            // Related standards: others in the same category (exclude current), titles only for sidebar
            var categoryNames = standard.Categories?
                .Where(c => !string.IsNullOrWhiteSpace(c.Name))
                .Select(c => c.Name!)
                .Distinct()
                .ToList() ?? [];
            if (categoryNames.Count > 0)
            {
                var relatedResponse = await _apiService.GetPublishedStandardsAsync(categories: categoryNames, page: 1, pageSize: 50);
                var related = relatedResponse?.Data?
                    .Where(s => !string.Equals(s.Slug, slug, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(s => s.Title)
                    .ToList() ?? [];
                ViewBag.RelatedStandards = related;
            }
            else
            {
                ViewBag.RelatedStandards = new List<DdtStandardDto>();
            }

            return View("~/Views/Standards/DdtStandards/Details.cshtml", standard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading DDT standard with slug {Slug}", slug);
            ViewBag.ErrorMessage = "An error occurred while loading the standard. Please try again later.";
            return View("~/Views/Standards/DdtStandards/Details.cshtml", (DdtStandardDetailDto?)null);
        }
    }
}
