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
                var nav = await _navigationService.GetNavigationAsync();
                var path = context.HttpContext.Request.Path.Value ?? string.Empty;
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
}
