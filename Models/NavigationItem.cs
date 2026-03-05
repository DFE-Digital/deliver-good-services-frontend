namespace ServiceManual.Models
{
    public class NavigationItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Order { get; set; }
        public string? Url { get; set; }
        public List<NavigationItem> Children { get; set; } = [];
    }
}
