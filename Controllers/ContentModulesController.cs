using Microsoft.AspNetCore.Mvc;
using ServiceManual.Models;
using ServiceManual.Services;

namespace ServiceManual.Controllers
{
    /// <summary>Lists every CMS content module (content-entry) in alphabetical order.</summary>
    public class ContentModulesController : Controller
    {
        private readonly ICmsApiService _cmsApiService;

        public ContentModulesController(ICmsApiService cmsApiService)
        {
            _cmsApiService = cmsApiService;
        }

        [Route("content-modules")]
        public async Task<IActionResult> Index()
        {
            var modules = await _cmsApiService.GetAllContentModulesAlphabeticalAsync();
            return View(modules);
        }
    }
}
