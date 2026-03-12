using Microsoft.AspNetCore.Mvc;
using ServiceManual.Helpers;
using ServiceManual.Services;

namespace ServiceManual.Controllers
{
    public class CollectionController : Controller
    {
        private readonly ICmsApiService _cmsApiService;
        private readonly DdtStandardsApiService _standardsApiService;

        public CollectionController(ICmsApiService cmsApiService, DdtStandardsApiService standardsApiService)
        {
            _cmsApiService = cmsApiService;
            _standardsApiService = standardsApiService;
        }

        [Route("guidance/collections/{slug}")]
        public async Task<IActionResult> Show(string slug)
        {
            var collection = await _cmsApiService.GetCollectionBySlugAsync(slug);

            if (collection is null)
                return NotFound();

            var bodyResolved = await GovUkMarkdownHelper.ReplaceDdtStandardCodeShortcodesAsync(collection.Body, _standardsApiService);
            ViewBag.CollectionBodyHtml = GovUkMarkdownHelper.ToGovUkHtmlForBody(bodyResolved);
            ViewBag.Collection = collection;
            ViewBag.PageNotification = await _cmsApiService.GetActivePageNotificationAsync("collections", slug);

            return View("~/Views/Templates/Collection.cshtml");
        }
    }
}
