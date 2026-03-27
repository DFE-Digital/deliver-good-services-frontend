using Microsoft.AspNetCore.Mvc;

namespace ServiceManual.Controllers
{
    public class LibraryController : Controller
    {
        [Route("my-library")]
        public IActionResult Index()
        {
            ViewData["Title"] = "My library";
            return View("~/Views/Library/Index.cshtml");
        }
    }
}
