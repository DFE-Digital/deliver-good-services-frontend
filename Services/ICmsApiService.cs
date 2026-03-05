using ServiceManual.Models;

namespace ServiceManual.Services
{
    public interface ICmsApiService
    {
        Task<SinglePageGuide?> GetSinglePageGuideBySlugAsync(string slug);
        Task<Collection?> GetCollectionBySlugAsync(string slug);
        Task<DetailedGuide?> GetDetailedGuideBySlugAsync(string slug);
        Task<DetailedGuidePage?> GetDetailedGuidePageBySlugAsync(string guideSlug, string pageSlug);
        Task<List<NavigationItem>> GetNavigationAsync();
        Task<HtmlPage?> GetHtmlPageBySlugAsync(string slug);
        Task<Lifecycle?> GetLifecycleAsync();
        /// <summary>Gets a lifecycle by slug (e.g. "service-delivery-lifecycle"). New collection model with stages and task placements.</summary>
        Task<Lifecycle?> GetLifecycleBySlugAsync(string slug);
        Task<Phase?> GetPhaseBySlugAsync(string slug);
        Task<List<ContentIndexItem>> GetAllPublishedContentAsync();

        /// <summary>Looks up a published redirect by short URL. Returns the URL to redirect to, or null if not found.</summary>
        Task<string?> GetRedirectUrlByShortUrlAsync(string shortUrl);
    }
}
