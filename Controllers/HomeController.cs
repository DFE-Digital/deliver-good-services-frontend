using Microsoft.AspNetCore.Mvc;
using ServiceManual.Services;

namespace ServiceManual.Controllers;

public class HomeController : Controller
{
    private readonly ICmsApiService _cmsApiService;

    public HomeController(ICmsApiService cmsApiService)
    {
        _cmsApiService = cmsApiService;
    }

    /// <summary>Default route; also used as safe fallback from <see cref="MovedController"/>.</summary>
    public IActionResult Index()
    {
        return Redirect("/guidance");
    }

    [HttpGet("/roadmap")]
    public async Task<IActionResult> Roadmap()
    {
        var roadmap = await _cmsApiService.GetRoadmapAsync();
        if (roadmap is null)
            return NotFound();

        ViewBag.Roadmap = roadmap;
        ViewData["Title"] = roadmap.Title;
        return View("~/Views/Roadmap/Index.cshtml");
    }

    [HttpGet("/contact")]
    public IActionResult Contact()
    {
        ViewData["Title"] = "Contact";
        return View("~/Views/Home/Contact.cshtml");
    }

    public IActionResult Error()
    {
        ViewData["Title"] = "Error";
        return View("~/Views/Home/Error.cshtml");
    }
}
