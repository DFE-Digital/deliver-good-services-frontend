namespace ServiceManual.Models
{
    /// <summary>Unified view model for both the guide overview page and child pages.</summary>
    public class GuidePageViewModel
    {
        public bool IsOverviewPage { get; set; }
        public string HeroTitle { get; set; } = string.Empty;
        public string? HeroIntro { get; set; }
        public string? CollectionSlug { get; set; }
        public string? CollectionTitle { get; set; }
        public List<CollectionRef> Collections { get; set; } = [];
        public bool ShowLastReviewedDateOnPage { get; set; }
        public string? LastReviewedDateDisplay { get; set; }
        public string? Owner { get; set; }
        public string? OwnerUrl { get; set; }
        public List<TagRef> AudienceTags { get; set; } = [];
        /// <summary>When false, hide the contents nav (only used on overview when HideContentsOnPrimaryPage).</summary>
        public bool ShowContents { get; set; } = true;
        /// <summary>When true, show numbered list (guide with sub pages). When false, show hyphens (body headings only).</summary>
        public bool ContentsUseNumbers { get; set; } = true;
        public List<GuideContentsItem> ContentsItems { get; set; } = [];
        public string? BodyHtml { get; set; }
        /// <summary>When true, show the page title (h2) and optional intro above the body (child pages only).</summary>
        public bool ShowPageHeader { get; set; }
        public string? PageTitle { get; set; }
        public string? PageBeforeContentsHtml { get; set; }
        public string? PaginationPrevUrl { get; set; }
        public string? PaginationPrevLabel { get; set; }
        public string? PaginationNextUrl { get; set; }
        public string? PaginationNextLabel { get; set; }
        public List<RelatedContentItem> RelatedContent { get; set; } = [];
        /// <summary>Related files for the Downloads sidebar section.</summary>
        public List<RelatedFileItem> RelatedFiles { get; set; } = [];
        /// <summary>When true, apply guide-content-section--no-contents (overview only, when contents hidden).</summary>
        public bool ApplyNoContentsSectionStyle { get; set; }
    }

    public class GuideContentsItem
    {
        public int Number { get; set; }
        public string Title { get; set; } = string.Empty;
        /// <summary>When null, render as span (current page).</summary>
        public string? Url { get; set; }
        public bool IsCurrent { get; set; }
    }
}
