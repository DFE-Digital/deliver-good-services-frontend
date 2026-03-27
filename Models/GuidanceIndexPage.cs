namespace ServiceManual.Models
{
    public class GuidanceIndexPage
    {
        public List<GuidanceAreaGroup> Areas { get; set; } = [];
    }

    public class GuidanceAreaGroup
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public string? Description { get; set; }
        public string ColourHex { get; set; } = "#1d70b8";
        public List<TagRef> FeaturedProfessions { get; set; } = [];
        public List<GuidanceCollectionCard> Collections { get; set; } = [];
        public int CollectionCount => Collections.Count;
        public int TotalItemCount => Collections.Sum(collection => collection.ItemCount);
        public string IntroText => !string.IsNullOrWhiteSpace(Description)
            ? Description!
            : Summary ?? string.Empty;
    }

    public class GuidanceCollectionCard
    {
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public bool Featured { get; set; }
        public List<string> Tags { get; set; } = [];
        public List<TagRef> ApplicableProfessions { get; set; } = [];
        public List<CollectionRef> AlsoInAreas { get; set; } = [];
    }

    public class GuidanceIndexViewModel
    {
        public List<GuidanceAreaGroup> Areas { get; set; } = [];
        public List<GuidanceFilterOption> AreaFilters { get; set; } = [];
        public List<GuidanceFilterOption> ProfessionFilters { get; set; } = [];
        public string? SelectedGuidanceAreaSlug { get; set; }
        public List<string> SelectedProfessionSlugs { get; set; } = [];
        public int TotalCollectionCount { get; set; }
        public bool HasActiveFilters =>
            !string.IsNullOrWhiteSpace(SelectedGuidanceAreaSlug) || SelectedProfessionSlugs.Count > 0;
    }

    public class GuidanceFilterOption
    {
        public string Label { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public int Count { get; set; }
        public string? ColourHex { get; set; }
    }
}