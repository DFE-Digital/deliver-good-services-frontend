namespace ServiceManual.Models
{
    public class ServiceStandardSummary
    {
        public string Title { get; set; } = string.Empty;
        public int Point { get; set; }
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class ServiceStandardPage
    {
        public string Title { get; set; } = string.Empty;
        public int Point { get; set; }
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Body { get; set; }
        public List<ServiceStandardSection> Sections { get; set; } = [];
    }

    public class ServiceStandardSection
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<ContentEntry> ContentEntries { get; set; } = [];
    }
}
