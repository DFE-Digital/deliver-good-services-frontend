namespace ServiceManual.Models
{
    public class Roadmap
    {
        public string Title { get; set; } = string.Empty;
        public string? MetaDescription { get; set; }
        public string? Body { get; set; }
        public string? UpdateHistory { get; set; }
    }
}