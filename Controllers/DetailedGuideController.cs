using Microsoft.AspNetCore.Mvc;
using ServiceManual.Helpers;
using ServiceManual.Models;
using ServiceManual.Services;
using System.Text;

namespace ServiceManual.Controllers
{
    public class DetailedGuideController : Controller
    {
        private readonly ICmsApiService _cmsApiService;
        private readonly IBlobMetadataProvider _blobMetadataProvider;
        private readonly DdtStandardsApiService _standardsApiService;

        public DetailedGuideController(ICmsApiService cmsApiService, IBlobMetadataProvider blobMetadataProvider, DdtStandardsApiService standardsApiService)
        {
            _cmsApiService = cmsApiService;
            _blobMetadataProvider = blobMetadataProvider;
            _standardsApiService = standardsApiService;
        }

        [Route("guidance/guides/{slug}")]
        public async Task<IActionResult> Show(string slug)
        {
            var guide = await _cmsApiService.GetDetailedGuideBySlugAsync(slug);

            if (guide is null)
                return NotFound();

            var bodyResolved = await GovUkMarkdownHelper.ReplaceDdtStandardCodeShortcodesAsync(guide.Body, _standardsApiService);
            var model = BuildViewModelFromGuide(guide, bodyResolved);
            if (!string.IsNullOrEmpty(model.BodyHtml))
                model.BodyHtml = await GovUkMarkdownHelper.EnrichDocumentListWithBlobMetadataAsync(model.BodyHtml, _blobMetadataProvider);
            ViewBag.GuidePage = model;
            ViewBag.PageNotification = await _cmsApiService.GetActivePageNotificationAsync("detailed_guides", slug);
            return View("~/Views/Templates/DetailedGuide.cshtml");
        }

        [Route("guidance/guides/{guideSlug}/{pageSlug}")]
        public async Task<IActionResult> ShowPage(string guideSlug, string pageSlug)
        {
            var guidePage = await _cmsApiService.GetDetailedGuidePageBySlugAsync(guideSlug, pageSlug);

            if (guidePage is null)
                return NotFound();

            var bodyResolved = await GovUkMarkdownHelper.ReplaceDdtStandardCodeShortcodesAsync(guidePage.Body, _standardsApiService);
            var beforeContentsResolved = !string.IsNullOrEmpty(guidePage.BeforeContents)
                ? await GovUkMarkdownHelper.ReplaceDdtStandardCodeShortcodesAsync(guidePage.BeforeContents, _standardsApiService)
                : null;
            var model = BuildViewModelFromGuidePage(guidePage, bodyResolved, beforeContentsResolved);
            if (!string.IsNullOrEmpty(model.BodyHtml))
                model.BodyHtml = await GovUkMarkdownHelper.EnrichDocumentListWithBlobMetadataAsync(model.BodyHtml, _blobMetadataProvider);
            ViewBag.GuidePage = model;
            ViewBag.PageNotification = await _cmsApiService.GetActivePageNotificationAsync("detailed_guide_pages", pageSlug);

            return View("~/Views/Templates/DetailedGuide.cshtml");
        }

        private static GuidePageViewModel BuildViewModelFromGuide(DetailedGuide guide, string? resolvedBody = null)
        {
            var bodyMarkdown = resolvedBody ?? guide.Body;
            bodyMarkdown = GovUkMarkdownHelper.ReplaceServiceStandardListShortcode(bodyMarkdown, guide.Slug, guide.Pages);
            var bodyHtml = !string.IsNullOrEmpty(bodyMarkdown)
                ? GovUkMarkdownHelper.ToGovUkHtmlForBody(bodyMarkdown)
                : "";
            var overviewTitle = string.IsNullOrWhiteSpace(guide.OverrideOverviewTitle)
                ? "Overview"
                : guide.OverrideOverviewTitle.Trim();

            List<GuideContentsItem> contents;
            bool showContents;
            bool contentsUseNumbers;
            if (guide.Pages.Count > 0)
            {
                showContents = !guide.HideContentsOnPrimaryPage;
                contentsUseNumbers = true;
                contents = new List<GuideContentsItem>
                {
                    new() { Number = 1, Title = overviewTitle, Url = null, IsCurrent = true }
                };
                for (var i = 0; i < guide.Pages.Count; i++)
                {
                    var p = guide.Pages[i];
                    contents.Add(new GuideContentsItem
                    {
                        Number = i + 2,
                        Title = p.Title,
                        Url = $"/guidance/guides/{guide.Slug}/{p.Slug}",
                        IsCurrent = false,
                        Phases = p.Phases,
                        Professions = p.Professions
                    });
                }
            }
            else
            {
                var bodyHeadings = GovUkMarkdownHelper.ExtractHeadingsFromHtml(bodyHtml);
                showContents = !guide.HideContentsOnPrimaryPage && bodyHeadings.Count > 0;
                contentsUseNumbers = false;
                contents = bodyHeadings
                    .Select((h, i) => new GuideContentsItem
                    {
                        Number = i + 1,
                        Title = h.Text,
                        Url = "#" + h.Id,
                        IsCurrent = false
                    })
                    .ToList();
            }

            return new GuidePageViewModel
            {
                IsOverviewPage = true,
                HeroTitle = guide.Title,
                HeroIntro = guide.MetaDescription,
                CollectionSlug = guide.CollectionSlug,
                CollectionTitle = guide.CollectionTitle,
                Collections = guide.Collections,
                ShowLastReviewedDateOnPage = guide.ShowLastReviewedDateOnPage,
                LastReviewedDateDisplay = guide.LastReviewedDateDisplay,
                ShowOwnerOnPage = guide.ShowOwnerOnPage,
                Owner = guide.Owner,
                OwnerUrl = guide.OwnerUrl,
                ShowApplicablePhasesOnPage = guide.ShowApplicablePhasesOnPage,
                PhaseTags = guide.PhaseTags,
                ShowApplicableProfessionsOnPage = guide.ShowApplicableProfessionsOnPage,
                AudienceTags = guide.AudienceTags,
                ShowContents = showContents,
                ContentsUseNumbers = contentsUseNumbers,
                ContentsItems = contents,
                OverviewTitle = overviewTitle,
                BodyHtml = bodyHtml,
                ShowPageHeader = true,
                PageTitle = overviewTitle,
                PaginationNextUrl = guide.Pages.Count > 0 ? $"/guidance/guides/{guide.Slug}/{guide.Pages[0].Slug}" : null,
                PaginationNextLabel = guide.Pages.Count > 0 ? guide.Pages[0].Title : null,
                RelatedContent = guide.RelatedContent,
                RelatedFiles = guide.RelatedFiles,
                ShowGuidePagesOnRight = false,
                GuidePagesRightNav = [],
                ApplyNoContentsSectionStyle = guide.HideContentsOnPrimaryPage,
                CustomCSS = guide.CustomCSS,
                CustomJS = guide.CustomJS,
            };
        }

        private static GuidePageViewModel BuildViewModelFromGuidePage(DetailedGuidePage guidePage, string? resolvedBody = null, string? resolvedBeforeContents = null)
        {
            var overviewTitle = string.IsNullOrWhiteSpace(guidePage.OverrideOverviewTitle)
                ? "Overview"
                : guidePage.OverrideOverviewTitle.Trim();

            var professionsTagsHtml = "";
            if (guidePage.Professions.Count > 0)
            {
                var sb = new System.Text.StringBuilder();
                sb.Append("<div class=\"ss-row__meta\">");
                foreach (var p in guidePage.Professions)
                {
                    sb.Append("<span class=\"ss-role\">");
                    sb.Append(System.Net.WebUtility.HtmlEncode(p));
                    sb.Append("</span>");
                }
                sb.Append("</div>");
                professionsTagsHtml = sb.ToString();
            }

            var phasesTagsHtml = "";
            if (guidePage.Phases.Count > 0)
            {
                var sbPh = new System.Text.StringBuilder();
                sbPh.Append("<div class=\"ss-row__meta\">");
                foreach (var phase in guidePage.Phases)
                {
                    var cls = GovUkMarkdownHelper.PhaseClassFromSlug(phase.Slug);
                    sbPh.Append("<span class=\"ss-ph");
                    if (!string.IsNullOrEmpty(cls)) { sbPh.Append(' '); sbPh.Append(cls); }
                    sbPh.Append("\">");
                    sbPh.Append(System.Net.WebUtility.HtmlEncode(phase.Title));
                    sbPh.Append("</span>");
                }
                sbPh.Append("</div>");
                phasesTagsHtml = sbPh.ToString();
            }

            var bodyMarkdown = (resolvedBody ?? guidePage.Body ?? "").Replace("[[professions]]", professionsTagsHtml).Replace("[[phases]]", phasesTagsHtml);
            bodyMarkdown = GovUkMarkdownHelper.ReplaceServiceStandardListShortcode(bodyMarkdown, guidePage.GuideSlug, guidePage.SiblingPages);
            var bodyHtml = GovUkMarkdownHelper.ToGovUkHtmlForBody(bodyMarkdown);
            var beforeContentsRaw = resolvedBeforeContents ?? guidePage.BeforeContents ?? "";
            var beforeContentsHtml = !string.IsNullOrWhiteSpace(beforeContentsRaw)
                ? GovUkMarkdownHelper.ToGovUkHtmlForBody(GovUkMarkdownHelper.ReplaceServiceStandardListShortcode(beforeContentsRaw.Replace("[[professions]]", professionsTagsHtml).Replace("[[phases]]", phasesTagsHtml), guidePage.GuideSlug, guidePage.SiblingPages))
                : null;

            var contents = new List<GuideContentsItem>
            {
                new() { Number = 1, Title = overviewTitle, Url = $"/guidance/guides/{guidePage.GuideSlug}", IsCurrent = false }
            };
            var currentIndex = guidePage.SiblingPages.FindIndex(p => p.Slug == guidePage.Slug);
            for (var i = 0; i < guidePage.SiblingPages.Count; i++)
            {
                var s = guidePage.SiblingPages[i];
                var isCurrent = s.Slug == guidePage.Slug;
                contents.Add(new GuideContentsItem
                {
                    Number = i + 2,
                    Title = s.Title,
                    Url = isCurrent ? null : $"/guidance/guides/{guidePage.GuideSlug}/{s.Slug}",
                    IsCurrent = isCurrent
                });
            }

            string? prevUrl = null, prevLabel = null;
            if (currentIndex == 0)
            {
                prevUrl = $"/guidance/guides/{guidePage.GuideSlug}";
                prevLabel = overviewTitle;
            }
            else if (currentIndex > 0)
            {
                var prev = guidePage.SiblingPages[currentIndex - 1];
                prevUrl = $"/guidance/guides/{guidePage.GuideSlug}/{prev.Slug}";
                prevLabel = prev.Title;
            }

            string? nextUrl = null, nextLabel = null;
            if (currentIndex >= 0 && currentIndex < guidePage.SiblingPages.Count - 1)
            {
                var next = guidePage.SiblingPages[currentIndex + 1];
                nextUrl = $"/guidance/guides/{guidePage.GuideSlug}/{next.Slug}";
                nextLabel = next.Title;
            }

            var showContents = !guidePage.HideTitleAndDescription
                && !guidePage.HideContents
                && !string.IsNullOrEmpty(guidePage.GuideTitle)
                && guidePage.SiblingPages.Count >= 1;

            var currentPageSummary = guidePage.SiblingPages.FirstOrDefault(p => string.Equals(p.Slug, guidePage.Slug, StringComparison.OrdinalIgnoreCase));
            var summaryPhases = currentPageSummary?.Phases?
                .Where(p => !string.IsNullOrWhiteSpace(p.Title))
                .GroupBy(p => (p.Slug ?? string.Empty).Trim().ToLowerInvariant() + "|" + (p.Title ?? string.Empty).Trim().ToLowerInvariant())
                .Select(g => g.First())
                .ToList() ?? [];
            var summaryProfessions = currentPageSummary?.Professions?
                .Where(p => !string.IsNullOrWhiteSpace(p.Title))
                .Select(p => p.Title!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? [];

            if (summaryProfessions.Count == 0 && guidePage.Professions.Count > 0)
            {
                summaryProfessions = guidePage.Professions
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Select(p => p.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            var contentGroupTabs = BuildContentGroupTabs(guidePage.Sections);

            return new GuidePageViewModel
            {
                IsOverviewPage = false,
                HeroTitle = guidePage.GuideTitle ?? "",
                HeroIntro = !string.IsNullOrWhiteSpace(guidePage.MetaDescription)
                    ? guidePage.MetaDescription
                    : guidePage.GuideMetaDescription,
                CollectionSlug = guidePage.CollectionSlug,
                CollectionTitle = guidePage.CollectionTitle,
                Collections = guidePage.Collections,
                ShowLastReviewedDateOnPage = guidePage.ShowLastReviewedDateOnPage,
                LastReviewedDateDisplay = guidePage.LastReviewedDateDisplay,
                ShowOwnerOnPage = guidePage.ShowOwnerOnPage,
                Owner = guidePage.Owner,
                OwnerUrl = guidePage.OwnerUrl,
                ShowApplicablePhasesOnPage = guidePage.ShowApplicablePhasesOnPage,
                PhaseTags = guidePage.PhaseTags,
                ShowApplicableProfessionsOnPage = guidePage.ShowApplicableProfessionsOnPage,
                AudienceTags = guidePage.AudienceTags,
                ShowContents = showContents,
                ContentsUseNumbers = true,
                ContentsItems = contents,
                OverviewTitle = overviewTitle,
                BodyHtml = bodyHtml,
                SummaryPhasesPrimarilyAssessedAt = summaryPhases,
                SummaryProfessionsResponsible = summaryProfessions,
                ShowPageHeader = !guidePage.HideTitleAndDescription || !string.IsNullOrWhiteSpace(guidePage.BeforeContents),
                PageTitle = guidePage.Title,
                PageBeforeContentsHtml = beforeContentsHtml,
                PaginationPrevUrl = prevUrl,
                PaginationPrevLabel = prevLabel,
                PaginationNextUrl = nextUrl,
                PaginationNextLabel = nextLabel,
                RelatedContent = guidePage.RelatedContent,
                RelatedFiles = guidePage.RelatedFiles,
                ShowGuidePagesOnRight = guidePage.ShowGuidePagesOnRight,
                GuidePagesRightNav = guidePage.SiblingPages
                    .Select((p, i) => new GuidePageRightNavItem
                    {
                        Number = i + 1,
                        Title = p.Title,
                        Url = string.Equals(p.Slug, guidePage.Slug, StringComparison.OrdinalIgnoreCase)
                            ? null
                            : $"/guidance/guides/{guidePage.GuideSlug}/{p.Slug}",
                        IsCurrent = string.Equals(p.Slug, guidePage.Slug, StringComparison.OrdinalIgnoreCase)
                    })
                    .ToList(),
                ContentGroupTabs = contentGroupTabs,
                ApplyNoContentsSectionStyle = guidePage.HideContents || guidePage.HideTitleAndDescription,
                CustomCSS = guidePage.CustomCSS,
                CustomJS = guidePage.CustomJS,
            };
        }

        private static List<GuidePageContentGroupTab> BuildContentGroupTabs(List<DetailedGuidePageSection> sections)
        {
            var result = new List<GuidePageContentGroupTab>();
            var indexByGroup = new Dictionary<string, GuidePageContentGroupTab>(StringComparer.OrdinalIgnoreCase);

            foreach (var section in sections)
            {
                var sectionTitle = string.IsNullOrWhiteSpace(section.Title) ? "Section" : section.Title.Trim();
                var groupName = string.IsNullOrWhiteSpace(section.Group) ? "General" : section.Group.Trim();
                var groupKey = groupName.ToLowerInvariant();

                if (!indexByGroup.TryGetValue(groupKey, out var groupTab))
                {
                    var panelBase = "guide-group-" + Slugify(groupName);
                    var panelId = panelBase;
                    var duplicate = 2;
                    while (result.Any(g => string.Equals(g.PanelId, panelId, StringComparison.OrdinalIgnoreCase)))
                    {
                        panelId = panelBase + "-" + duplicate;
                        duplicate++;
                    }

                    groupTab = new GuidePageContentGroupTab
                    {
                        GroupName = groupName,
                        PanelId = panelId
                    };
                    indexByGroup[groupKey] = groupTab;
                    result.Add(groupTab);
                }

                groupTab.Sections.Add(new GuidePageContentGroupSection
                {
                    Title = sectionTitle,
                    BodyHtml = string.IsNullOrWhiteSpace(section.Body)
                        ? null
                        : GovUkMarkdownHelper.ToGovUkHtmlForBody(section.Body),
                    Modules = section.ContentModules
                });
            }

            return result;
        }

        private static string Slugify(string value)
        {
            var builder = new StringBuilder();
            var previousDash = false;

            foreach (var ch in value.Trim().ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(ch))
                {
                    builder.Append(ch);
                    previousDash = false;
                }
                else if (!previousDash)
                {
                    builder.Append('-');
                    previousDash = true;
                }
            }

            return builder.ToString().Trim('-');
        }

    }
}
