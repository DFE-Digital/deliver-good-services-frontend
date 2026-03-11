namespace ServiceManual.Models
{
    /// <summary>A related file for the Downloads sidebar section (name, url, size, type, optional caption).</summary>
    public class RelatedFileItem
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        /// <summary>Human-readable size (e.g. "1.2 MB", "500 KB").</summary>
        public string SizeDisplay { get; set; } = string.Empty;
        /// <summary>File type / extension for display (e.g. "PDF", "DOCX").</summary>
        public string FileType { get; set; } = string.Empty;
        /// <summary>Optional caption from the CMS.</summary>
        public string? Caption { get; set; }
    }
}
