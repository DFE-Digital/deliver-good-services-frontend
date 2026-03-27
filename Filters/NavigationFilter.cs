using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ServiceManual.Models;
using ServiceManual.Services;

namespace ServiceManual.Filters
{
    public class NavigationFilter : IAsyncActionFilter
    {
        private readonly INavigationService _navigationService;
        private readonly ICmsApiService _cmsApiService;

        public NavigationFilter(INavigationService navigationService, ICmsApiService cmsApiService)
        {
            _navigationService = navigationService;
            _cmsApiService = cmsApiService;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var resultContext = await next();

            if (resultContext.Controller is Controller controller)
            {
                var path = context.HttpContext.Request.Path.Value ?? string.Empty;

                if (path.StartsWith("/documentation", StringComparison.OrdinalIgnoreCase))
                {
                    var sections = await _cmsApiService.GetDocumentationSectionsAsync();
                    controller.ViewData["Navigation"] = BuildDocumentationNavigation(sections);
                    controller.ViewData["IsDocumentationNav"] = true;
                }
                else
                {
                    var nav = await _navigationService.GetNavigationAsync();
                    if (path == "/lifecycle" || path.StartsWith("/lifecycle/", StringComparison.OrdinalIgnoreCase))
                    {
                        var lifecycle = await _cmsApiService.GetLifecycleAsync();
                        if (lifecycle?.Stages?.Count > 0)
                        {
                            var orderedStages = lifecycle.Stages.OrderBy(s => s.Order).ToList();
                            var lifecycleNav = new NavigationItem
                            {
                                Title = "What to do when",
                                Url = "/lifecycle",
                                Children =
                                [
                                    new NavigationItem { Title = "Overview", Url = "/lifecycle", Order = 0 },
                                    ..orderedStages.Select((s, i) => new NavigationItem { Title = s.Title, Url = $"/lifecycle#{s.Slug}", Order = i + 1 })
                                ]
                            };
                            nav = [lifecycleNav, .. nav];
                        }
                    }
                    if (nav is null || nav.Count == 0)
                    {
                        nav = GetDefaultSiteNavigation();
                    }

                    controller.ViewData["Navigation"] = nav;
                }
            }
        }

        private static List<NavigationItem> GetDefaultSiteNavigation()
        {
            return
            [
                new NavigationItem { Title = "Browse guidance", Url = "/content", Order = 0 },
                new NavigationItem { Title = "Documentation", Url = "/documentation", Order = 1 },
                new NavigationItem { Title = "DDT standards", Url = "/standards/ddt-standards", Order = 2 }
            ];
        }

        private static List<NavigationItem> BuildDocumentationNavigation(List<DocumentationSection> sections)
        {
            var nav = new List<NavigationItem>
            {
                new() { Title = "Back to site", Url = "/", Order = 0 },
                new() { Title = "Documentation", Url = "/documentation", Order = 1 }
            };
            var order = 2;
            foreach (var section in sections)
            {
                nav.Add(new NavigationItem
                {
                    Title = section.Title,
                    Url = $"/documentation/{section.Slug}",
                    Order = order++
                });
            }
            return nav;
        }
    }
}
