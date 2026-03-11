using Microsoft.AspNetCore.Mvc;
using ServiceManual.Helpers;
using ServiceManual.Models;
using ServiceManual.Services;

namespace ServiceManual.Controllers
{
    public class DetailedGuideController : Controller
    {
        private readonly ICmsApiService _cmsApiService;

        public DetailedGuideController(ICmsApiService cmsApiService)
        {
            _cmsApiService = cmsApiService;
        }

        [Route("guidance/guides/{slug}")]
        public async Task<IActionResult> Show(string slug)
        {
            var guide = await _cmsApiService.GetDetailedGuideBySlugAsync(slug);

            if (guide is null)
                return NotFound();

            var model = BuildViewModelFromGuide(guide);
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

            var model = BuildViewModelFromGuidePage(guidePage);
            ViewBag.GuidePage = model;
            ViewBag.PageNotification = await _cmsApiService.GetActivePageNotificationAsync("detailed_guide_pages", pageSlug);

            return View("~/Views/Templates/DetailedGuide.cshtml");
        }

        private static GuidePageViewModel BuildViewModelFromGuide(DetailedGuide guide)
        {
            var bodyHtml = !string.IsNullOrEmpty(guide.Body)
                ? GovUkMarkdownHelper.ToGovUkHtmlForBody(guide.Body)
                : "";

            List<GuideContentsItem> contents;
            bool showContents;
            bool contentsUseNumbers;
            if (guide.Pages.Count > 0)
            {
                showContents = !guide.HideContentsOnPrimaryPage;
                contentsUseNumbers = true;
                contents = new List<GuideContentsItem>
                {
                    new() { Number = 1, Title = "Overview", Url = null, IsCurrent = true }
                };
                for (var i = 0; i < guide.Pages.Count; i++)
                {
                    var p = guide.Pages[i];
                    contents.Add(new GuideContentsItem
                    {
                        Number = i + 2,
                        Title = p.Title,
                        Url = $"/guidance/guides/{guide.Slug}/{p.Slug}",
                        IsCurrent = false
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
                ShowLastUpdatedDateOnPage = guide.ShowLastUpdatedDateOnPage,
                UpdatedAtDisplay = guide.UpdatedAtDisplay,
                LastReviewedDateDisplay = guide.LastReviewedDateDisplay,
                Owner = guide.Owner,
                OwnerUrl = guide.OwnerUrl,
                AudienceTags = guide.AudienceTags,
                ShowContents = showContents,
                ContentsUseNumbers = contentsUseNumbers,
                ContentsItems = contents,
                BodyHtml = bodyHtml,
                ShowPageHeader = true,
                PageTitle = "Overview",
                PaginationNextUrl = guide.Pages.Count > 0 ? $"/guidance/guides/{guide.Slug}/{guide.Pages[0].Slug}" : null,
                PaginationNextLabel = guide.Pages.Count > 0 ? guide.Pages[0].Title : null,
                RelatedContent = guide.RelatedContent,
                RelatedFiles = guide.RelatedFiles,
                ApplyNoContentsSectionStyle = guide.HideContentsOnPrimaryPage
            };
        }

        private static GuidePageViewModel BuildViewModelFromGuidePage(DetailedGuidePage guidePage)
        {
            var professionsTagsHtml = "";
            if (guidePage.Professions.Count > 0)
            {
                var sb = new System.Text.StringBuilder();
                foreach (var p in guidePage.Professions)
                {
                    sb.Append("<strong class=\"govuk-tag govuk-!-margin-right-2 govuk-!-margin-bottom-2\">");
                    sb.Append(System.Net.WebUtility.HtmlEncode(p));
                    sb.Append("</strong>");
                }
                professionsTagsHtml = sb.ToString();
            }

            var bodyForRender = (guidePage.Body ?? "").Replace("[[professions]]", professionsTagsHtml);
            var bodyHtml = GovUkMarkdownHelper.ToGovUkHtmlForBody(bodyForRender);
            var beforeContentsHtml = !string.IsNullOrWhiteSpace(guidePage.BeforeContents)
                ? GovUkMarkdownHelper.ToGovUkHtmlForBody(guidePage.BeforeContents.Replace("[[professions]]", professionsTagsHtml))
                : null;

            var contents = new List<GuideContentsItem>
            {
                new() { Number = 1, Title = "Overview", Url = $"/guidance/guides/{guidePage.GuideSlug}", IsCurrent = false }
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
                prevLabel = "Overview";
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
                && !string.IsNullOrEmpty(guidePage.GuideTitle)
                && guidePage.SiblingPages.Count >= 1;

            return new GuidePageViewModel
            {
                IsOverviewPage = false,
                HeroTitle = guidePage.GuideTitle ?? "",
                HeroIntro = guidePage.GuideMetaDescription,
                CollectionSlug = guidePage.CollectionSlug,
                CollectionTitle = guidePage.CollectionTitle,
                Collections = guidePage.Collections,
                ShowLastUpdatedDateOnPage = guidePage.ShowLastUpdatedDateOnPage,
                UpdatedAtDisplay = guidePage.UpdatedAtDisplay,
                LastReviewedDateDisplay = guidePage.LastReviewedDateDisplay,
                Owner = guidePage.Owner,
                OwnerUrl = guidePage.OwnerUrl,
                AudienceTags = guidePage.AudienceTags,
                ShowContents = showContents,
                ContentsUseNumbers = true,
                ContentsItems = contents,
                BodyHtml = bodyHtml,
                ShowPageHeader = !guidePage.HideTitleAndDescription || !string.IsNullOrWhiteSpace(guidePage.BeforeContents),
                PageTitle = guidePage.Title,
                PageBeforeContentsHtml = beforeContentsHtml,
                PaginationPrevUrl = prevUrl,
                PaginationPrevLabel = prevLabel,
                PaginationNextUrl = nextUrl,
                PaginationNextLabel = nextLabel,
                RelatedContent = guidePage.RelatedContent,
                RelatedFiles = guidePage.RelatedFiles,
                ApplyNoContentsSectionStyle = false
            };
        }
    }
}
