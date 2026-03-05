using Microsoft.AspNetCore.Mvc;

namespace ServiceManual.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
            return View();
    }

    public IActionResult Error()
    {
        return View();
    }
}

