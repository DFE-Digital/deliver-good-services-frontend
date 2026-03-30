using System.Text.Json;

namespace ServiceManual.Models
{
    public class HowManyPeoplePage
    {
        public string Title { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public string? Description { get; set; }
        public string? QuickPickNumbers { get; set; }
        public string? DataDisclaimer { get; set; }
        public JsonElement? Data { get; set; }
    }
}
