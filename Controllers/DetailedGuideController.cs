using Microsoft.AspNetCore.Mvc;
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

            var firstPage = guide.Pages.FirstOrDefault();

            if (firstPage is null)
                return NotFound();

            return RedirectToAction(nameof(ShowPage), new { guideSlug = slug, pageSlug = firstPage.Slug });
        }

        [Route("guidance/guides/{guideSlug}/{pageSlug}")]
        public async Task<IActionResult> ShowPage(string guideSlug, string pageSlug)
        {
            var guidePage = await _cmsApiService.GetDetailedGuidePageBySlugAsync(guideSlug, pageSlug);

            if (guidePage is null)
                return NotFound();

            ViewBag.Page = guidePage;
            ViewBag.PageNotification = await _cmsApiService.GetActivePageNotificationAsync("detailed_guide_pages", pageSlug);

            return View("~/Views/Templates/DetailedGuidePage.cshtml");
        }
    }
}
