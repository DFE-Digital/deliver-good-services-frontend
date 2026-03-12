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
        var contents = new List<GuideContentsItem>
        {
            new() { Number = 1, Title = "Overview", Url = null, IsCurrent = true }
        };
        for (var i = 0; i < sections.Count; i++)
        {
            var sec = sections[i];
            contents.Add(new GuideContentsItem
            {
                Number = i + 2,
                Title = sec.Title,
                Url = $"/documentation/{sec.Slug}",
                IsCurrent = false
            });
        }
        ViewData["DocSections"] = sections;
        ViewData["DocContentsItems"] = contents;
        ViewData["DocHeroTitle"] = "Documentation";
        ViewData["DocHeroIntro"] = "Styles, components, patterns, CMS templates, publishing lifecycle, and configuration for the DfE Service Manual. Content is managed in the CMS.";
        return View("~/Views/Documentation/Index.cshtml");
    }

    [Route("documentation/{sectionSlug}")]
    [HttpGet]
    public async Task<IActionResult> Section(string sectionSlug)
    {
        var section = await _cmsApiService.GetDocumentationSectionBySlugAsync(sectionSlug);
        if (section is null)
            return NotFound();

        var contents = new List<GuideContentsItem>
        {
            new() { Number = 1, Title = "Overview", Url = null, IsCurrent = true }
        };
        for (var i = 0; i < section.Documentations.Count; i++)
        {
            var p = section.Documentations[i];
            contents.Add(new GuideContentsItem
            {
                Number = i + 2,
                Title = p.Title,
                Url = $"/documentation/{section.Slug}/{p.Slug}",
                IsCurrent = false
            });
        }
        ViewData["DocSection"] = section;
        ViewData["DocContentsItems"] = contents;
        ViewData["DocHeroTitle"] = section.Title;
        ViewData["DocHeroIntro"] = $"Pages in this section. {section.Documentations.Count} page{(section.Documentations.Count == 1 ? "" : "s")}.";
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
        var pages = section?.Documentations ?? new List<DocumentationPageSummary>();

        var contents = new List<GuideContentsItem>
        {
            new() { Number = 1, Title = "Overview", Url = $"/documentation/{sectionSlug}", IsCurrent = false }
        };
        var currentIndex = -1;
        for (var i = 0; i < pages.Count; i++)
        {
            var p = pages[i];
            var isCurrent = string.Equals(p.Slug, pageSlug, StringComparison.OrdinalIgnoreCase);
            if (isCurrent) currentIndex = i;
            contents.Add(new GuideContentsItem
            {
                Number = i + 2,
                Title = p.Title,
                Url = $"/documentation/{sectionSlug}/{p.Slug}",
                IsCurrent = isCurrent
            });
        }

        string? prevUrl = null, prevLabel = null, nextUrl = null, nextLabel = null;
        if (currentIndex >= 0)
        {
            if (currentIndex > 0)
            {
                var prev = pages[currentIndex - 1];
                prevUrl = $"/documentation/{sectionSlug}/{prev.Slug}";
                prevLabel = prev.Title;
            }
            else
            {
                prevUrl = $"/documentation/{sectionSlug}";
                prevLabel = "Overview";
            }
            if (currentIndex < pages.Count - 1)
            {
                var next = pages[currentIndex + 1];
                nextUrl = $"/documentation/{sectionSlug}/{next.Slug}";
                nextLabel = next.Title;
            }
        }

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
            SectionTitle = page.SectionTitle,
            ContentsItems = contents,
            PaginationPrevUrl = prevUrl,
            PaginationPrevLabel = prevLabel,
            PaginationNextUrl = nextUrl,
            PaginationNextLabel = nextLabel
        };
        return View("~/Views/Documentation/Page.cshtml", model);
    }
}
