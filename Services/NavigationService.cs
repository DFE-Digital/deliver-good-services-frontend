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
        private readonly ILogger<NavigationService> _logger;

        private const string CacheKey = "site_navigation";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(1);

        public NavigationService(ICmsApiService cmsApiService, IMemoryCache cache, ILogger<NavigationService> logger)
        {
            _cmsApiService = cmsApiService;
            _cache = cache;
            _logger = logger;
        }

        public async Task<List<NavigationItem>> GetNavigationAsync()
        {
            if (_cache.TryGetValue(CacheKey, out List<NavigationItem>? cached) && cached is not null)
                return cached;

            var nav = await _cmsApiService.GetNavigationAsync();

            _cache.Set(CacheKey, nav, CacheDuration);

            return nav;
        }
    }
}
