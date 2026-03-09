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
                    controller.ViewData["Navigation"] = GetDocumentationNavigation();
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
                            nav = [lifecycleNav, ..nav];
                        }
                    }
                    controller.ViewData["Navigation"] = nav;
                }
            }
        }

        private static List<NavigationItem> GetDocumentationNavigation()
        {
            return
            [
                new NavigationItem { Title = "Back to site", Url = "/", Order = 0 },
                new NavigationItem { Title = "Documentation", Url = "/documentation", Order = 1 },
                new NavigationItem { Title = "Styles", Url = "/documentation/styles", Order = 2 },
                new NavigationItem { Title = "Components", Url = "/documentation/components", Order = 3 },
                new NavigationItem { Title = "Patterns", Url = "/documentation/patterns", Order = 4 },
                new NavigationItem { Title = "Templates", Url = "/documentation/templates", Order = 5 },
                new NavigationItem { Title = "Publishing", Url = "/documentation/publishing", Order = 6 },
                new NavigationItem { Title = "Configuration", Url = "/documentation/configuration", Order = 7 }
            ];
        }
    }
}
