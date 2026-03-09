using ServiceManual.Models;

namespace ServiceManual.Services
{
    public interface ICmsApiService
    {
        Task<SinglePageGuide?> GetSinglePageGuideBySlugAsync(string slug);
        Task<Collection?> GetCollectionBySlugAsync(string slug);
        Task<JobSpecification?> GetJobSpecificationBySlugAsync(string slug);
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

        /// <summary>Looks up a published 301 redirect by old path. Returns the redirect rule, or null if not found.</summary>
        Task<PathRedirect?> GetPathRedirectByOldPathAsync(string path);

        /// <summary>Gets an active page notification for the given content type and slug, when validFrom &lt;= now &lt;= validTo and enabled is true. relationFilter: single_page_guides, collections, detailed_guides, or detailed_guide_pages.</summary>
        Task<PageNotification?> GetActivePageNotificationAsync(string relationFilter, string slug);
    }
}
