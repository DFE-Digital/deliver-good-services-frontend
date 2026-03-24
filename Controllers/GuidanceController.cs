using Microsoft.AspNetCore.Mvc;

namespace ServiceManual.Controllers;

public class GuidanceController : Controller
{
    [Route("guidance")]
    public IActionResult Index()
    {
        return Redirect("/content");
    }
}
