using Microsoft.AspNetCore.Mvc;
using ServiceManual.Services;

namespace ServiceManual.Controllers
{
    public class GuidanceController : Controller
    {
        private readonly ICmsApiService _cmsApiService;

        public GuidanceController(ICmsApiService cmsApiService)
        {
            _cmsApiService = cmsApiService;
        }

        [Route("guidance/{slug}")]
        public async Task<IActionResult> Show(string slug)
        {
            var guide = await _cmsApiService.GetSinglePageGuideBySlugAsync(slug);

            if (guide is null)
                return NotFound();

            ViewBag.Guide = guide;
            ViewBag.PageNotification = await _cmsApiService.GetActivePageNotificationAsync("single_page_guides", slug);

            return View("~/Views/Templates/SinglePageGuide.cshtml");
        }
    }
}
