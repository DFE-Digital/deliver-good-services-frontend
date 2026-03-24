using Microsoft.Extensions.Caching.Memory;
using ServiceManual.Models;

namespace ServiceManual.Services
{
    public interface INavigationService
    {
        Task<List<NavigationItem>> GetNavigationAsync();
    }

    public class NavigationService : INavigationService
    {
        private readonly ICmsApiService _cmsApiService;
        private readonly IMemoryCache _cache;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly ILogger<NavigationService> _logger;

        private const string CacheKey = "site_navigation";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

        public NavigationService(ICmsApiService cmsApiService, IMemoryCache cache, IHostEnvironment hostEnvironment, ILogger<NavigationService> logger)
        {
            _cmsApiService = cmsApiService;
            _cache = cache;
            _hostEnvironment = hostEnvironment;
            _logger = logger;
        }

        public async Task<List<NavigationItem>> GetNavigationAsync()
        {
            // Always fetch fresh nav in Development so CMS edits appear immediately.
            if (!_hostEnvironment.IsDevelopment() && _cache.TryGetValue(CacheKey, out List<NavigationItem>? cached) && cached is not null)
                return cached;

            var nav = await _cmsApiService.GetNavigationAsync();

            // Do not cache an empty nav response; this avoids sticky fallback menus when CMS/permissions are transiently unavailable.
            if (nav.Count > 0)
            {
                _cache.Set(CacheKey, nav, CacheDuration);
            }
            else
            {
                _cache.Remove(CacheKey);
                _logger.LogWarning("NavigationService: CMS navigation result was empty; skipping cache write.");
            }

            return nav;
        }
    }
}
