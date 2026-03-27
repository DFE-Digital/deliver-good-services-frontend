namespace ServiceManual.Models
{
    public class CollectionSection
    {
        public string Title { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public List<ContentLink> Items { get; set; } = [];
    }

    public class ContentLink
    {
        public string Title { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public string? MetaDescription { get; set; }
        public string Url { get; set; } = string.Empty;
        /// <summary>When true, link should open in a new tab (e.g. external links).</summary>
        public bool OpenInNewTab { get; set; }
        /// <summary>Display label for content type (e.g. "Guidance", "External link") for collection listing.</summary>
        public string? ContentType { get; set; }
        /// <summary>Grade for job specification items (e.g. "G7", "SEO").</summary>
        public string? Grade { get; set; }
    }
}
