namespace ServiceManual.Models
{
    /// <summary>
    /// Reference to a tagged phase or profession (tags-phase / tags-profession) for content indexing and display.
    /// </summary>
    public class TagRef
    {
        public string Slug { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }
}
