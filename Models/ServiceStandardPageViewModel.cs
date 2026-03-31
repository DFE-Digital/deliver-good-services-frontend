namespace ServiceManual.Models
{
    public class ServiceStandardPageViewModel
    {
        public ServiceStandardPage Standard { get; set; } = new();
        public ServicePointGuidanceViewModel Guidance { get; set; } = new();
        public List<ServiceStandardSummary> AllStandards { get; set; } = [];
        public string CurrentSlug { get; set; } = string.Empty;
        public string? PreviousUrl { get; set; }
        public string? PreviousLabel { get; set; }
        public string? NextUrl { get; set; }
        public string? NextLabel { get; set; }
    }
}
