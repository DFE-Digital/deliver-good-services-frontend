namespace ServiceManual.Models
{
    /// <summary>
    /// A single lifecycle phase (e.g. Discovery, Alpha). Powers the phase landing page.
    /// Spec: Lifecycle &amp; Phase CMS Content Model §3.2, §4
    /// </summary>
    public class Phase
    {
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public int Sequence { get; set; }
        public string? Hint { get; set; }
        public string? Summary { get; set; }
        public string? Purpose { get; set; }
        public string? Intent { get; set; }
        public string? Duration { get; set; }
        public string? TeamSize { get; set; }
        public bool IsActive { get; set; } = true;
        public string? LifecycleTitle { get; set; }
        public string? LifecycleSlug { get; set; }
        public List<string> TrackTitles { get; set; } = [];

        public List<PhaseActivity> Activities { get; set; } = [];
        public List<PhaseRequirement> MandatoryRequirements { get; set; } = [];
        public List<PhaseOutput> Outputs { get; set; } = [];
        public PhaseAssurance? Assurance { get; set; }

        /// <summary>Guidance items where applicable_phases includes this phase (from CMS).</summary>
        public List<PhaseRelatedLink> RelatedGuidance { get; set; } = [];
        public List<PhaseRelatedLink> RelatedStandards { get; set; } = [];
        public List<PhaseRelatedLink> RelatedTools { get; set; } = [];
        public List<PhaseRelatedLink> RelatedTraining { get; set; } = [];
    }

    public class PhaseActivity
    {
        public string Title { get; set; } = string.Empty;
        public string? ShortDescription { get; set; }
        public string? Description { get; set; }
        public string? GuidanceContent { get; set; }
        public List<string> ProfessionTitles { get; set; } = [];
        public string? TrackTitle { get; set; }
        public List<PhaseRelatedLink> RelatedContent { get; set; } = [];
        public List<PhaseActivityResource> Resources { get; set; } = [];
    }

    public class PhaseRequirement
    {
        public string RequirementTitle { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? EvidenceExamples { get; set; }
        public PhaseRelatedLink? RelatedStandard { get; set; }
    }

    public class PhaseOutput
    {
        public string OutputName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public PhaseRelatedLink? TemplateLink { get; set; }
        public bool IsMandatory { get; set; }
    }

    public class PhaseAssurance
    {
        public string AssuranceType { get; set; } = string.Empty; // Peer review / Service assessment / None
        public string? Description { get; set; }
        public string? GovernanceBody { get; set; }
        public PhaseRelatedLink? PreparationGuidance { get; set; }
    }

    public class PhaseRelatedLink
    {
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }

    public class PhaseActivityResource
    {
        public string ResourceType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Url { get; set; }
        public string? MediaUrl { get; set; }
    }
}
