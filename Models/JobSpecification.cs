namespace ServiceManual.Models
{
    public class JobSpecification
    {
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Grade { get; set; }
        public string? RoleDescription { get; set; }
        public string? Skills { get; set; }
        public bool EnableWordDocDownload { get; set; }
        public JobSpecificationProfession? Profession { get; set; }
        public List<JobSpecificationSibling> SiblingJobSpecifications { get; set; } = [];
    }

    public class JobSpecificationProfession
    {
        public string? Title { get; set; }
        public string? Slug { get; set; }
        /// <summary>Plural form of the profession name (e.g. "service designers") for headings like "What service designers do".</summary>
        public string? Plural { get; set; }
        /// <summary>Richtext/markdown description for the profession (shown at top of job description page).</summary>
        public string? ProfessionDescription { get; set; }
    }

    public class JobSpecificationSibling
    {
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Grade { get; set; }
    }
}
