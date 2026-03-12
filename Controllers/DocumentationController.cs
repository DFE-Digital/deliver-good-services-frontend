using Microsoft.AspNetCore.Mvc;
using ServiceManual.Helpers;
using ServiceManual.Models;
using ServiceManual.Services;

namespace ServiceManual.Controllers;

public class DocumentationController : Controller
{
    private readonly ICmsApiService _cmsApiService;
    private readonly DdtStandardsApiService _standardsApiService;

    public DocumentationController(ICmsApiService cmsApiService, DdtStandardsApiService standardsApiService)
    {
        _cmsApiService = cmsApiService;
        _standardsApiService = standardsApiService;
    }

    [Route("documentation")]
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var sections = await _cmsApiService.GetDocumentationSectionsAsync();
        ViewData["DocSections"] = sections;
        ViewData["CurrentDocSectionSlug"] = (string?)null;
        ViewData["DocSection"] = (DocumentationSection?)null;
        ViewData["DocPages"] = new List<DocumentationPageSummary>();
        return View("~/Views/Documentation/Index.cshtml");
    }

    [Route("documentation/{sectionSlug}")]
    [HttpGet]
    public async Task<IActionResult> Section(string sectionSlug)
    {
        var section = await _cmsApiService.GetDocumentationSectionBySlugAsync(sectionSlug);
        if (section is null)
            return NotFound();

        var sections = await _cmsApiService.GetDocumentationSectionsAsync();
        ViewData["DocSections"] = sections;
        ViewData["CurrentDocSectionSlug"] = section.Slug;
        ViewData["DocSection"] = section;
        ViewData["DocPages"] = section.Documentations;
        ViewData["DocSectionTitle"] = section.Title;
        return View("~/Views/Documentation/SectionIndex.cshtml", section);
    }

    [Route("documentation/{sectionSlug}/{pageSlug}")]
    [HttpGet]
    public async Task<IActionResult> Page(string sectionSlug, string pageSlug)
    {
        var page = await _cmsApiService.GetDocumentationBySlugAsync(sectionSlug, pageSlug);
        if (page is null)
            return NotFound();

        var section = await _cmsApiService.GetDocumentationSectionBySlugAsync(sectionSlug);
        var sections = await _cmsApiService.GetDocumentationSectionsAsync();
        ViewData["DocSections"] = sections;
        ViewData["CurrentDocSectionSlug"] = sectionSlug;
        ViewData["DocSection"] = section;
        ViewData["DocPages"] = section?.Documentations ?? new List<DocumentationPageSummary>();
        ViewData["DocSectionTitle"] = page.SectionTitle;

        var bodyResolved = await GovUkMarkdownHelper.ReplaceDdtStandardCodeShortcodesAsync(page.Body, _standardsApiService);
        var bodyHtml = string.IsNullOrEmpty(bodyResolved)
            ? string.Empty
            : GovUkMarkdownHelper.ToGovUkHtmlForBody(bodyResolved);

        var model = new DocumentationPageViewModel
        {
            Title = page.Title,
            Slug = page.Slug,
            MetaDescription = page.MetaDescription,
            BodyHtml = bodyHtml,
            LastReviewedDateDisplay = page.LastReviewedDateDisplay,
            SectionSlug = page.SectionSlug,
            SectionTitle = page.SectionTitle
        };
        return View("~/Views/Documentation/Page.cshtml", model);
    }
}
