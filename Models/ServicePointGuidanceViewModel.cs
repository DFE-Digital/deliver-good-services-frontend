namespace ServiceManual.Models
{
    public class ServicePointGuidanceViewModel
    {
        public string? IntroHtml { get; set; }
        public string ActivePhaseSlug { get; set; } = string.Empty;
        public string? SelectedRoleSlug { get; set; }
        public string? SelectedStrength { get; set; }
        public List<TagRef> PhaseTabs { get; set; } = [];
        public List<TagRef> AvailableRoles { get; set; } = [];
        public List<string> AvailableStrengths { get; set; } = [];
        public List<ContentEntry> Entries { get; set; } = [];
    }
}
