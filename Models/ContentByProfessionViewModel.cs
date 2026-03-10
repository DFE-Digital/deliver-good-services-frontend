namespace ServiceManual.Models
{
    /// <summary>
    /// View model for the content-by-profession page (all content tagged with a given profession).
    /// </summary>
    public class ContentByProfessionViewModel
    {
        public string ProfessionTitle { get; set; } = string.Empty;
        public string ProfessionSlug { get; set; } = string.Empty;
        public IReadOnlyList<ContentIndexItem> Items { get; set; } = [];
    }
}
