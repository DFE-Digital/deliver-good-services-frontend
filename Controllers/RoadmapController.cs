using Microsoft.AspNetCore.Mvc;
using ServiceManual.Services;

namespace ServiceManual.Controllers
{
    public class RoadmapController : Controller
    {
        private readonly ICmsApiService _cmsApiService;

        public RoadmapController(ICmsApiService cmsApiService)
        {
            _cmsApiService = cmsApiService;
        }

        [Route("roadmap")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var roadmap = await _cmsApiService.GetRoadmapAsync();

            if (roadmap is null)
                return NotFound();

            ViewBag.Roadmap = roadmap;

            return View("~/Views/Roadmap/Index.cshtml");
        }
    }
}