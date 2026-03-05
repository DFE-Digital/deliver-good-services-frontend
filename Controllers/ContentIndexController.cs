using Microsoft.AspNetCore.Mvc;
using ServiceManual.Services;

namespace ServiceManual.Controllers
{
    /// <summary>
    /// Lists all published CMS content with title, meta description, type and link to the appropriate view.
    /// </summary>
    public class ContentIndexController : Controller
    {
        private readonly ICmsApiService _cmsApiService;

        public ContentIndexController(ICmsApiService cmsApiService)
        {
            _cmsApiService = cmsApiService;
        }

        [Route("content")]
        public async Task<IActionResult> Index()
        {
            var items = await _cmsApiService.GetAllPublishedContentAsync();
            return View(items);
        }
    }
}
