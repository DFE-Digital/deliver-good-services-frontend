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
        public bool GuidePagesOnRightSide { get; set; }
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
    }
}
