using Microsoft.AspNetCore.Mvc;
using ServiceManual.Services;

namespace ServiceManual.Controllers
{
    public class LifecycleController : Controller
    {
        private readonly ICmsApiService _cmsApiService;

        public LifecycleController(ICmsApiService cmsApiService)
        {
            _cmsApiService = cmsApiService;
        }

        /// <summary>
        /// Lifecycle index: "What to do when". All sections are loaded from the CMS:
        /// lifecycle, lifecycle-stage, stage-task-placement, task, track, role, output.
        /// Route: /lifecycle (spec §6)
        /// </summary>
        [Route("lifecycle")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var lifecycle = await _cmsApiService.GetLifecycleAsync();
            // No fallback: only CMS content is shown. If null, view shows empty state.
            ViewBag.Lifecycle = lifecycle;
            return View("~/Views/Lifecycle/Index.cshtml");
        }

        /// <summary>
        /// Redirect "What to do when" URL to lifecycle.
        /// </summary>
        [Route("what-to-do-when")]
        [HttpGet]
        public IActionResult WhatToDoWhenRedirect()
        {
            return RedirectPermanent("/lifecycle");
        }

        /// <summary>
        /// Phase detail page (e.g. /lifecycle/discovery). If CMS has phase content, show Phase view; otherwise redirect to hub with hash.
        /// </summary>
        [Route("lifecycle/{slug}")]
        [HttpGet]
        public async Task<IActionResult> Phase(string slug)
        {
            var phase = await _cmsApiService.GetPhaseBySlugAsync(slug);
            if (phase != null)
            {
                var lifecycle = await _cmsApiService.GetLifecycleAsync();
                var allPhases = lifecycle?.Stages?.OrderBy(s => s.Order).Select(s => s.Slug.ToLowerInvariant()).ToList() ?? new List<string>();
                ViewBag.Phase = phase;
                ViewBag.AllPhases = allPhases;
                return View("~/Views/Lifecycle/Phase.cshtml");
            }
            return RedirectPermanent($"/lifecycle#ph-{slug.ToLowerInvariant()}");
        }
    }
}
