namespace ServiceManual.Models
{
    public class DetailedGuide
    {
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? MetaDescription { get; set; }
        /// <summary>Richtext body for the guide landing page (replaces the previous first child page).</summary>
        public string? Body { get; set; }
        /// <summary>Optional replacement label for "Overview" on guide navigation and headings.</summary>
        public string? OverrideOverviewTitle { get; set; }
        public string? CollectionTitle { get; set; }
        public string? CollectionSlug { get; set; }
        /// <summary>All collections this guide is part of; first is primary ("Part of"), rest are "Also part of".</summary>
        public List<CollectionRef> Collections { get; set; } = [];
        /// <summary>When true, hide the contents list on the guide's primary (overview) page.</summary>
        public bool HideContentsOnPrimaryPage { get; set; }
        /// <summary>When true, show detailed guide pages navigation on the right sidebar.</summary>
        public bool ShowGuidePagesOnRight { get; set; }
        public List<DetailedGuidePageSummary> Pages { get; set; } = [];
        public List<RelatedContentItem> RelatedContent { get; set; } = [];
        /// <summary>When true, show "Last reviewed: [date]" under the meta description.</summary>
        public bool ShowLastReviewedDateOnPage { get; set; }
        /// <summary>Formatted last reviewed date for display (e.g. "7 January 2026").</summary>
        public string? LastReviewedDateDisplay { get; set; }
        /// <summary>When true, show owner in the meta strip.</summary>
        public bool ShowOwnerOnPage { get; set; } = true;
        /// <summary>Owner label for meta (e.g. from CMS).</summary>
        public string? Owner { get; set; }
        /// <summary>When set, the owner is linked (e.g. from content owner's informationPage redirector).</summary>
        public string? OwnerUrl { get; set; }
        /// <summary>When true, show applicable phases in the meta strip.</summary>
        public bool ShowApplicablePhasesOnPage { get; set; }
        /// <summary>Applicable phases for meta strip (phase slug + title).</summary>
        public List<TagRef> PhaseTags { get; set; } = [];
        /// <summary>When true, show applicable professions (audience) in the meta strip.</summary>
        public bool ShowApplicableProfessionsOnPage { get; set; }
        /// <summary>Audience / applicable professions for meta (tags-profession titles).</summary>
        public List<string> Audience { get; set; } = [];
        /// <summary>Audience as tag refs (slug + title) for linking to content-by-profession view.</summary>
        public List<TagRef> AudienceTags { get; set; } = [];
        /// <summary>Related files for the Downloads sidebar section.</summary>
        public List<RelatedFileItem> RelatedFiles { get; set; } = [];
        /// <summary>Custom CSS to inject into the page &lt;style&gt; block.</summary>
        public string? CustomCSS { get; set; }
        /// <summary>Custom JavaScript to inject into the page &lt;script&gt; block.</summary>
        public string? CustomJS { get; set; }
        /// <summary>When true, show the unpublished draft banner under the hero.</summary>
        public bool ShowDraftContentBanner { get; set; }
    }

    public class DetailedGuidePageSummary
    {
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? MetaDescription { get; set; }
        public List<TagRef> Phases { get; set; } = [];
        public List<TagRef> Professions { get; set; } = [];
    }
}
