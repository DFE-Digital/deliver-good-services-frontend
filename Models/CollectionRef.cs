namespace ServiceManual.Models
{
    /// <summary>Reference to a collection (title and slug) for "Part of" / "Also part of" display.</summary>
    public class CollectionRef
    {
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
    }
}
