namespace ServiceManual.Models
{
    /// <summary>
    /// Summary of a published CMS content item for the content index page.
    /// </summary>
    public class ContentIndexItem
    {
        public string Title { get; set; } = string.Empty;
        public string? MetaDescription { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        /// <summary>Slug for grouping (e.g. detailed guide slug so pages can nest under it).</summary>
        public string? Slug { get; set; }
        /// <summary>Parent type for nesting (e.g. "Detailed Guide", "Lifecycle").</summary>
        public string? ParentContentType { get; set; }
        /// <summary>Parent slug when parent is identified by slug (e.g. guide slug for detailed guide pages).</summary>
        public string? ParentSlug { get; set; }
        /// <summary>When this content is part of a collection, the collection title (for display).</summary>
        public string? CollectionTitle { get; set; }
        /// <summary>When this content is part of a collection, the collection slug (for link).</summary>
        public string? CollectionSlug { get; set; }
        /// <summary>Tagged phases (tags-phase) this content is applicable to, for "Content by tagged phase" index.</summary>
        public List<TagRef> ApplicablePhaseTags { get; set; } = [];
        /// <summary>Tagged professions (tags-profession) this content is applicable to, for "Content by tagged profession" index.</summary>
        public List<TagRef> ApplicableProfessionTags { get; set; } = [];
    }
}
