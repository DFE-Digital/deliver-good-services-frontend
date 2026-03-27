namespace ServiceManual.Models
{
    public class Article
    {
        public string RouteKey { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? MetaDescription { get; set; }
        public string? Body { get; set; }
        public string? Author { get; set; }
        public string? PublishedFromDisplay { get; set; }
        public string? PublishedToDisplay { get; set; }
        public string? LeadImageUrl { get; set; }
        public string? LeadImageAlt { get; set; }
    }

    public class ArticleSummary
    {
        public string RouteKey { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? MetaDescription { get; set; }
        public string? Author { get; set; }
        public string? PublishedFromDisplay { get; set; }
        public string? LeadImageUrl { get; set; }
        public string? LeadImageAlt { get; set; }
    }
}
