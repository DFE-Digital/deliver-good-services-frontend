namespace ServiceManual.Models
{
    public class ContentEntry
    {
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public string? Body { get; set; }
        public string? EntryType { get; set; }
        public string? Strength { get; set; }
        public string? Priority { get; set; }
        public bool LegalRequirement { get; set; }
        public string? Notes { get; set; }
        public List<TagRef> Phases { get; set; } = [];
        public List<TagRef> Roles { get; set; } = [];
    }
}
