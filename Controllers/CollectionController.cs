using Microsoft.AspNetCore.Mvc;
using ServiceManual.Services;

namespace ServiceManual.Controllers
{
    public class CollectionController : Controller
    {
        private readonly ICmsApiService _cmsApiService;

        public CollectionController(ICmsApiService cmsApiService)
        {
            _cmsApiService = cmsApiService;
        }

        [Route("guidance/collections/{slug}")]
        public async Task<IActionResult> Show(string slug)
        {
            var collection = await _cmsApiService.GetCollectionBySlugAsync(slug);

            if (collection is null)
                return NotFound();

            ViewBag.Collection = collection;

            return View("~/Views/Templates/Collection.cshtml");
        }
    }
}
