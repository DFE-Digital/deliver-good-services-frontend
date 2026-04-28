namespace ServiceManual.Models
{
    public class Collection
    {
        public string Title { get; set; } = string.Empty;
        public string MetaDescription { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Body { get; set; }
        public List<CollectionSection> Sections { get; set; } = [];
        public List<RelatedContentItem> RelatedContent { get; set; } = [];
        /// <summary>When true, show "Last reviewed: [date]" under the meta description.</summary>
        public bool ShowLastReviewedDateOnPage { get; set; }
        /// <summary>Formatted last reviewed date for display (e.g. "7 January 2026").</summary>
        public string? LastReviewedDateDisplay { get; set; }
        /// <summary>Content owner label for meta bar.</summary>
        public string? Owner { get; set; }
        /// <summary>When set, the owner is linked (e.g. from content owner's informationPage redirector).</summary>
        public string? OwnerUrl { get; set; }
        /// <summary>Audience (applicable professions) for meta bar; links to content-by-profession view.</summary>
        public List<TagRef> AudienceTags { get; set; } = [];
        /// <summary>Related files for the Downloads sidebar section.</summary>
        public List<RelatedFileItem> RelatedFiles { get; set; } = [];
        /// <summary>When true, show the unpublished draft banner (draft preview + Strapi entry has no <c>publishedAt</c>).</summary>
        public bool ShowDraftContentBanner { get; set; }
    }

    public class DetailedGuideSummary
    {
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? MetaDescription { get; set; }
    }
}
