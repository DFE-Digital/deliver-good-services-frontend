namespace ServiceManual.Models
{
    public class DetailedGuide
    {
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? MetaDescription { get; set; }
        /// <summary>Richtext body for the guide landing page (replaces the previous first child page).</summary>
        public string? Body { get; set; }
        public string? CollectionTitle { get; set; }
        public string? CollectionSlug { get; set; }
        /// <summary>All collections this guide is part of; first is primary ("Part of"), rest are "Also part of".</summary>
        public List<CollectionRef> Collections { get; set; } = [];
        /// <summary>When true, hide the contents list on the guide's primary (overview) page.</summary>
        public bool HideContentsOnPrimaryPage { get; set; }
        public List<DetailedGuidePageSummary> Pages { get; set; } = [];
        public List<RelatedContentItem> RelatedContent { get; set; } = [];
        /// <summary>When true, show "Last updated: [date]" under the meta description.</summary>
        public bool ShowLastUpdatedDateOnPage { get; set; }
        /// <summary>Formatted last updated date for display (e.g. "7 January 2026"), when ShowLastUpdatedDateOnPage is true.</summary>
        public string? UpdatedAtDisplay { get; set; }
        /// <summary>Formatted last reviewed date for display (e.g. "7 January 2026").</summary>
        public string? LastReviewedDateDisplay { get; set; }
        /// <summary>Owner label for meta (e.g. from CMS).</summary>
        public string? Owner { get; set; }
        /// <summary>When set, the owner is linked (e.g. from content owner's informationPage redirector).</summary>
        public string? OwnerUrl { get; set; }
        /// <summary>Audience / applicable professions for meta (tags-profession titles).</summary>
        public List<string> Audience { get; set; } = [];
        /// <summary>Audience as tag refs (slug + title) for linking to content-by-profession view.</summary>
        public List<TagRef> AudienceTags { get; set; } = [];
    }

    public class DetailedGuidePageSummary
    {
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
    }
}
