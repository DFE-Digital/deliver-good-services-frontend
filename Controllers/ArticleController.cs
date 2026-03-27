using Microsoft.AspNetCore.Mvc;
using ServiceManual.Helpers;
using ServiceManual.Services;

namespace ServiceManual.Controllers
{
    public class ArticleController : Controller
    {
        private readonly ICmsApiService _cmsApiService;

        public ArticleController(ICmsApiService cmsApiService)
        {
            _cmsApiService = cmsApiService;
        }

        [Route("guidance/articles")]
        public async Task<IActionResult> Index()
        {
            var articles = await _cmsApiService.GetArticlesAsync();
            return View("~/Views/Templates/Articles.cshtml", articles);
        }

        [Route("guidance/articles/{routeKey}")]
        public async Task<IActionResult> Show(string routeKey)
        {
            var article = await _cmsApiService.GetArticleByRouteKeyAsync(routeKey);
            if (article is null)
                return NotFound();

            ViewBag.BodyHtml = GovUkMarkdownHelper.ToGovUkHtmlForBody(article.Body ?? string.Empty);
            return View("~/Views/Templates/Article.cshtml", article);
        }
    }
}
