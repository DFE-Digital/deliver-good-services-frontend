using Microsoft.AspNetCore.Mvc;
using ServiceManual.Services;

namespace ServiceManual.Controllers
{
    public class ToolsController : Controller
    {
        private readonly ICmsApiService _cmsApiService;

        public ToolsController(ICmsApiService cmsApiService)
        {
            _cmsApiService = cmsApiService;
        }

        [Route("tools")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var page = await _cmsApiService.GetToolsPageAsync();

            if (page is null)
                return NotFound();

            ViewBag.ToolsPage = page;

            return View("~/Views/Tools/Index.cshtml");
        }

        [Route("tools/how-many-people/{population:int?}")]
        [HttpGet]
        public async Task<IActionResult> HowManyPeople(int? population)
        {
            var page = await _cmsApiService.GetHowManyPeoplePageAsync();

            if (page is null)
                return NotFound();

            ViewBag.HowManyPeoplePage = page;
            ViewBag.InitialPopulation = population is > 0 ? (int?)population.Value : null;

            return View("~/Views/Tools/HowManyPeople.cshtml");
        }
    }
}
