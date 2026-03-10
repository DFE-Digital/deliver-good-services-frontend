namespace ServiceManual.Models
{
    public class DetailedGuidePage
    {
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? MetaDescription { get; set; }
        public string? BeforeContents { get; set; }
        public string Body { get; set; } = string.Empty;
        public string? GuideTitle { get; set; }
        public string? GuideSlug { get; set; }
        public string? GuideMetaDescription { get; set; }
        /// <summary>When true, hide the contents list on the guide's primary (overview) page.</summary>
        public bool HideContentsOnPrimaryPage { get; set; }
        public bool HideTitleAndDescription { get; set; }
        public bool HideContents { get; set; }
        /// <summary>When true, the "Pages in this guide" sidebar is hidden (takes precedence over other rules).</summary>
        public bool HideGuidePagesNav { get; set; }
        public string? CollectionTitle { get; set; }
        public string? CollectionSlug { get; set; }
        /// <summary>All collections this page is part of; first is primary ("Part of"), rest are "Also part of".</summary>
        public List<CollectionRef> Collections { get; set; } = [];
        public List<DetailedGuidePageSummary> SiblingPages { get; set; } = [];
        public List<RelatedContentItem> RelatedContent { get; set; } = [];
        public List<string> Professions { get; set; } = [];
        /// <summary>When true, show "Last updated: [date]" under the meta description.</summary>
        public bool ShowLastUpdatedDateOnPage { get; set; }
        /// <summary>Formatted last updated date for display (e.g. "7 January 2026"), when ShowLastUpdatedDateOnPage is true.</summary>
        public string? UpdatedAtDisplay { get; set; }
        /// <summary>Formatted last reviewed date from the parent guide for display (e.g. "7 January 2026").</summary>
        public string? LastReviewedDateDisplay { get; set; }
        /// <summary>Owner from parent guide for meta strip.</summary>
        public string? Owner { get; set; }
        /// <summary>Owner link from parent guide (e.g. informationPage redirect).</summary>
        public string? OwnerUrl { get; set; }
        /// <summary>Audience (profession tags) from parent guide for meta strip.</summary>
        public List<TagRef> AudienceTags { get; set; } = [];
    }
}
