namespace ServiceManual.Models
{
    public class ToolsPage
    {
        public string Title { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public string? Description { get; set; }
        public string? Body { get; set; }
        public List<ToolsPageItem> Tools { get; set; } = [];
    }

    public class ToolsPageItem
    {
        public string Title { get; set; } = string.Empty;
        public string? Url { get; set; }
        public string? Description { get; set; }
        public bool InternalOnly { get; set; }
        public bool OpenInNewTab { get; set; }
    }
}
