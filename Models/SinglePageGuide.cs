namespace ServiceManual.Models
{
    public class SinglePageGuide
    {
        public string Title { get; set; } = string.Empty;
        public string MetaDescription { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string? LastReviewDate { get; set; }
        public string? NextReviewDate { get; set; }
        public List<RelatedContentItem> RelatedContent { get; set; } = [];
        /// <summary>Slug of the first collection this guide is part of (if any).</summary>
        public string? CollectionSlug { get; set; }
        /// <summary>Title of the first collection this guide is part of (if any).</summary>
        public string? CollectionTitle { get; set; }
        /// <summary>All collections this guide is part of; first is primary ("Part of"), rest are "Also part of".</summary>
        public List<CollectionRef> Collections { get; set; } = [];
        /// <summary>When true, show "Last updated: [date]" under the meta description.</summary>
        public bool ShowLastUpdatedDateOnPage { get; set; }
        /// <summary>Formatted last updated date for display (e.g. "7 January 2026"), when ShowLastUpdatedDateOnPage is true.</summary>
        public string? UpdatedAtDisplay { get; set; }
    }
}
