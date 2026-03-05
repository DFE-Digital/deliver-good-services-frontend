using Microsoft.AspNetCore.Mvc;
using ServiceManual.Services;

namespace ServiceManual.Controllers
{
    public class HtmlPageController : Controller
    {
        private readonly ICmsApiService _cmsApiService;

        public HtmlPageController(ICmsApiService cmsApiService)
        {
            _cmsApiService = cmsApiService;
        }

        [Route("pages/{slug}")]
        public async Task<IActionResult> Show(string slug)
        {
            var page = await _cmsApiService.GetHtmlPageBySlugAsync(slug);

            if (page is null)
                return NotFound();

            ViewBag.Page = page;

            return View("~/Views/Templates/HtmlPage.cshtml");
        }
    }
}
