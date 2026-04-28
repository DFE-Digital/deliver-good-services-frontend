using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using ServiceManual.Configuration;
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
        private readonly IConfiguration _configuration;
        private readonly ILogger<NavigationService> _logger;

        private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

        public NavigationService(
            ICmsApiService cmsApiService,
            IMemoryCache cache,
            IHostEnvironment hostEnvironment,
            IConfiguration configuration,
            ILogger<NavigationService> logger)
        {
            _cmsApiService = cmsApiService;
            _cache = cache;
            _hostEnvironment = hostEnvironment;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>Separate cache entries for draft vs published nav so <c>DraftPreview</c> toggles and CMS calls stay aligned.</summary>
        private string NavigationCacheKey =>
            _configuration.IsDraftPreviewEnabled()
                ? "site_navigation_draft"
                : "site_navigation_published";

        public async Task<List<NavigationItem>> GetNavigationAsync()
        {
            // Always fetch fresh nav in Development so CMS edits appear immediately.
            if (!_hostEnvironment.IsDevelopment() && _cache.TryGetValue(NavigationCacheKey, out List<NavigationItem>? cached) && cached is not null)
                return cached;

            var nav = await _cmsApiService.GetNavigationAsync();

            // Do not cache an empty nav response; this avoids sticky fallback menus when CMS/permissions are transiently unavailable.
            if (nav.Count > 0)
            {
                _cache.Set(NavigationCacheKey, nav, CacheDuration);
            }
            else
            {
                _cache.Remove(NavigationCacheKey);
                _logger.LogWarning("NavigationService: CMS navigation result was empty; skipping cache write.");
            }

            return nav;
        }
    }
}
