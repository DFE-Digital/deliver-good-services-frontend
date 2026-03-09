namespace ServiceManual.Models
{
    public class DetailedGuide
    {
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? MetaDescription { get; set; }
        public string? CollectionTitle { get; set; }
        public string? CollectionSlug { get; set; }
        /// <summary>All collections this guide is part of; first is primary ("Part of"), rest are "Also part of".</summary>
        public List<CollectionRef> Collections { get; set; } = [];
        public bool GuidePagesOnRightSide { get; set; }
        public List<DetailedGuidePageSummary> Pages { get; set; } = [];
        public List<RelatedContentItem> RelatedContent { get; set; } = [];
        /// <summary>When true, show "Last updated: [date]" under the meta description.</summary>
        public bool ShowLastUpdatedDateOnPage { get; set; }
        /// <summary>Formatted last updated date for display (e.g. "7 January 2026"), when ShowLastUpdatedDateOnPage is true.</summary>
        public string? UpdatedAtDisplay { get; set; }
    }

    public class DetailedGuidePageSummary
    {
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
    }
}
