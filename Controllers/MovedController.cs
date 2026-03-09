using Microsoft.AspNetCore.Mvc;

namespace ServiceManual.Controllers;

/// <summary>
/// Interim "content has moved" page for 301 redirects when "use interim page" is enabled.
/// Informs the user to update bookmarks, then redirects after 10 seconds or on click.
/// </summary>
public class MovedController : Controller
{
    /// <summary>
    /// Shows the interim page. Query param "to" is the destination URL (required).
    /// </summary>
    [Route("moved")]
    [HttpGet]
    public IActionResult Index([FromQuery] string? to)
    {
        if (string.IsNullOrWhiteSpace(to))
            return RedirectToAction(nameof(HomeController.Index), "Home");

        var newPath = to.Trim();
        var isAbsolute = Uri.TryCreate(newPath, UriKind.Absolute, out var uri)
                         && uri != null
                         && (uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase)
                             || uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase));
        var isRelative = newPath.StartsWith("/", StringComparison.Ordinal) && !newPath.StartsWith("//", StringComparison.Ordinal);
        if (!isAbsolute && !isRelative)
            return RedirectToAction(nameof(HomeController.Index), "Home");

        ViewBag.NewPath = newPath;
        ViewData["Title"] = "Content has moved";
        return View("~/Views/Moved/Index.cshtml");
    }
}
