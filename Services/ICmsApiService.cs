using ServiceManual.Models;

namespace ServiceManual.Services
{
    public interface ICmsApiService
    {
        Task<Collection?> GetCollectionBySlugAsync(string slug);
        Task<JobSpecification?> GetJobSpecificationBySlugAsync(string slug);
        Task<DetailedGuide?> GetDetailedGuideBySlugAsync(string slug);
        Task<DetailedGuidePage?> GetDetailedGuidePageBySlugAsync(string guideSlug, string pageSlug);
        Task<List<NavigationItem>> GetNavigationAsync();
        Task<HtmlPage?> GetHtmlPageBySlugAsync(string slug);
        Task<Roadmap?> GetRoadmapAsync();
        Task<Lifecycle?> GetLifecycleAsync();
        /// <summary>Gets a lifecycle by slug (e.g. "service-delivery-lifecycle"). New collection model with stages and task placements.</summary>
        Task<Lifecycle?> GetLifecycleBySlugAsync(string slug);
        Task<Phase?> GetPhaseBySlugAsync(string slug);
        Task<List<ContentIndexItem>> GetAllPublishedContentAsync();

        /// <summary>Looks up a published redirect by short URL. Returns the URL to redirect to, or null if not found.</summary>
        Task<string?> GetRedirectUrlByShortUrlAsync(string shortUrl);

        /// <summary>Looks up a published 301 redirect by old path. Returns the redirect rule, or null if not found.</summary>
        Task<PathRedirect?> GetPathRedirectByOldPathAsync(string path);

        /// <summary>Gets an active page notification for the given content type and slug, when validFrom &lt;= now &lt;= validTo and enabled is true. relationFilter: collections, detailed_guides, or detailed_guide_pages.</summary>
        Task<PageNotification?> GetActivePageNotificationAsync(string relationFilter, string slug);

        /// <summary>Gets all enabled documentation sections for the doc nav (top tabs), with page summaries for side nav.</summary>
        Task<List<DocumentationSection>> GetDocumentationSectionsAsync();

        /// <summary>Gets a single documentation section by slug (for section index and side nav).</summary>
        Task<DocumentationSection?> GetDocumentationSectionBySlugAsync(string sectionSlug);

        /// <summary>Gets a single documentation page by section and page slug.</summary>
        Task<DocumentationPage?> GetDocumentationBySlugAsync(string sectionSlug, string pageSlug);
    }
}
