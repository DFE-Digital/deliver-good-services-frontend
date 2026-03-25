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

    [Route("/")]
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        Console.WriteLine("HomeController.Index called");
        var homepage = await _cmsApiService.GetHomepageAsync();

        if (homepage is null)
            return NotFound();

        ViewBag.Homepage = homepage;

        return View("~/Views/Home/Index2.cshtml");
    }
}