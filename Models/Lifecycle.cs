namespace ServiceManual.Models
{
    /// <summary>
    /// Lifecycle (collection type): e.g. "What to do when". Has stages with placed tasks.
    /// Spec: strapi-lifecycle-content-model-spec.md
    /// </summary>
    public class Lifecycle
    {
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public string? OwnerLabel { get; set; }
        public string? AudienceLabel { get; set; }
        public string? LastUpdatedLabel { get; set; }
        public string DefaultView { get; set; } = "card"; // card | list
        public List<LinkCard> LucidResources { get; set; } = [];
        public List<LifecycleStage> Stages { get; set; } = [];
    }

    public class LinkCard
    {
        public string Label { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Url { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public string? BgColourHex { get; set; }
    }

    public class LifecycleStage
    {
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public string? DurationLabel { get; set; }
        public int Order { get; set; }
        public string? ColourHex { get; set; }
        public string? TagLabel { get; set; }
        public bool IsCollapsedByDefault { get; set; }
        public List<TrackRef> ActiveTracks { get; set; } = [];
        public List<StageTaskPlacement> Placements { get; set; } = [];
    }

    public class StageTaskPlacement
    {
        public string? WhenLabel { get; set; }
        public int Order { get; set; }
        public string? StageNotes { get; set; }
        public List<LinkItem> OverrideGuidanceLinks { get; set; } = [];
        public string Visibility { get; set; } = "published";
        public TaskRef Task { get; set; } = new();
        public List<OutputRef> OverrideOutputs { get; set; } = [];
    }

    public class TaskRef
    {
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? What { get; set; }
        public string? WhyItMatters { get; set; }
        public List<HowStep> HowSteps { get; set; } = [];
        public List<LinkItem> GuidanceLinks { get; set; } = [];
        public string? Notes { get; set; }
        public TrackRef? Track { get; set; }
        public List<RoleRef> Roles { get; set; } = [];
        public RoleRef? LeadRole { get; set; }
        public List<OutputRef> Outputs { get; set; } = [];
    }

    public class HowStep
    {
        public string StepText { get; set; } = string.Empty;
    }

    public class LinkItem
    {
        public string Label { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string? SourceLabel { get; set; }
        public bool OpensInNewTab { get; set; }
    }

    public class TrackRef
    {
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? ColourHex { get; set; }
    }

    public class RoleRef
    {
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? ShortCode { get; set; }
        public string? ColourHex { get; set; }
    }

    public class OutputRef
    {
        public string Title { get; set; } = string.Empty;
        public string? TemplateLink { get; set; }
    }
}
