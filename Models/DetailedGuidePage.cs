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
        /// <summary>Optional replacement label for "Overview" inherited from the parent guide.</summary>
        public string? OverrideOverviewTitle { get; set; }
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
        /// <summary>Applicable phases for this page (used for [[phases]] shortcode).</summary>
        public List<TagRef> Phases { get; set; } = [];
        public List<DetailedGuidePageSection> Sections { get; set; } = [];
        /// <summary>When true, show "Last reviewed: [date]" under the meta description.</summary>
        public bool ShowLastReviewedDateOnPage { get; set; }
        /// <summary>Formatted last reviewed date for display (e.g. "7 January 2026").</summary>
        public string? LastReviewedDateDisplay { get; set; }
        /// <summary>When true, show owner in the meta strip.</summary>
        public bool ShowOwnerOnPage { get; set; } = true;
        /// <summary>Owner from parent guide for meta strip.</summary>
        public string? Owner { get; set; }
        /// <summary>Owner link from parent guide (e.g. informationPage redirect).</summary>
        public string? OwnerUrl { get; set; }
        /// <summary>When true, show applicable phases in the meta strip.</summary>
        public bool ShowApplicablePhasesOnPage { get; set; }
        /// <summary>Applicable phases from parent guide for meta strip.</summary>
        public List<TagRef> PhaseTags { get; set; } = [];
        /// <summary>When true, show applicable professions (audience) in the meta strip.</summary>
        public bool ShowApplicableProfessionsOnPage { get; set; }
        /// <summary>Audience (profession tags) from parent guide for meta strip.</summary>
        public List<TagRef> AudienceTags { get; set; } = [];
        /// <summary>Related files for the Downloads sidebar section.</summary>
        public List<RelatedFileItem> RelatedFiles { get; set; } = [];
        /// <summary>Custom CSS inherited from the parent guide to inject into the page &lt;style&gt; block.</summary>
        public string? CustomCSS { get; set; }
        /// <summary>Custom JavaScript inherited from the parent guide to inject into the page &lt;script&gt; block.</summary>
        public string? CustomJS { get; set; }
    }

    public class DetailedGuidePageSection
    {
        public string Title { get; set; } = string.Empty;
        public string? Group { get; set; }
        public string? Body { get; set; }
        public List<ContentEntry> ContentModules { get; set; } = [];
    }
}
