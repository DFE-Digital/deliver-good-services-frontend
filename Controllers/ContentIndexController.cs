using Microsoft.AspNetCore.Mvc;
using ServiceManual.Models;
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

        /// <summary>
        /// GET /content/profession/{slug} — all content tagged with this profession (role).
        /// </summary>
        [Route("content/profession/{slug}")]
        public async Task<IActionResult> ByProfession(string slug)
        {
            var items = await _cmsApiService.GetAllPublishedContentAsync();
            var tag = items
                .SelectMany(i => i.ApplicableProfessionTags)
                .FirstOrDefault(t => string.Equals(t.Slug, slug, StringComparison.OrdinalIgnoreCase));
            if (tag == null)
                return NotFound();
            var professionItems = items
                .Where(i => i.ApplicableProfessionTags.Any(pt => string.Equals(pt.Slug, slug, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(i => i.Title, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var model = new ContentByProfessionViewModel
            {
                ProfessionTitle = tag.Title,
                ProfessionSlug = tag.Slug,
                Items = professionItems
            };
            return View(model);
        }
    }
}
