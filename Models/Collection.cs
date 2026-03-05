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
    }

    public class DetailedGuideSummary
    {
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? MetaDescription { get; set; }
    }
}
