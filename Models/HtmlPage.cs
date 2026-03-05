namespace ServiceManual.Models
{
    public class HtmlPage
    {
        public string Title { get; set; } = string.Empty;
        public string MetaDescription { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Html { get; set; } = string.Empty;
        public string? Css { get; set; }
        public string? Js { get; set; }
    }
}
