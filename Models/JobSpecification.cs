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
    }

    public class JobSpecificationSibling
    {
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Grade { get; set; }
    }
}
