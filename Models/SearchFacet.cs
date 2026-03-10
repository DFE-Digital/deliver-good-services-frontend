namespace ServiceManual.Models;

/// <summary>
/// A single facet for faceted search (e.g. content type with count from current results).
/// </summary>
public class SearchFacet
{
    public string ContentType { get; set; } = string.Empty;
    public int Count { get; set; }
}
