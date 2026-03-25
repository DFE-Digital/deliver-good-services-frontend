namespace ServiceManual.Models
{
    public class Homepage
    {
        public string Title { get; set; } = string.Empty;
        public string? Headline { get; set; }
        public string? Html { get; set; }
        public string? CustomJS { get; set; }
        public string? CustomCSS { get; set; }
    }
}