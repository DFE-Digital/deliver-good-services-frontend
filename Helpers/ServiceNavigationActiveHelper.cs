namespace ServiceManual.Helpers;

/// <summary>
/// Service nav active state: CMS items often point at the standards guidance collection
/// (<c>/guidance/collections/standards</c>) while DDT and service standard pages are served under <c>/standards/...</c>.
/// </summary>
public static class ServiceNavigationActiveHelper
{
    private const string StandardsCollectionNavPath = "/guidance/collections/standards";

    public static bool IsPathUnderStandardsApp(string currentPath)
    {
        if (string.IsNullOrEmpty(currentPath))
            return false;

        var trimmed = currentPath.TrimEnd('/');
        return string.Equals(trimmed, "/standards", StringComparison.OrdinalIgnoreCase)
               || currentPath.StartsWith("/standards/", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsServiceNavItemActive(string? itemUrl, string currentPath)
    {
        if (string.IsNullOrEmpty(itemUrl))
            return false;

        var baseUrl = itemUrl.TrimEnd('/');
        if (baseUrl.Length == 0)
            return false;

        var pathTrimmed = currentPath.TrimEnd('/');
        var prefixMatch = string.Equals(pathTrimmed, baseUrl, StringComparison.OrdinalIgnoreCase)
                          || currentPath.StartsWith(baseUrl + "/", StringComparison.OrdinalIgnoreCase);

        if (prefixMatch)
            return true;

        if (string.Equals(baseUrl, StandardsCollectionNavPath, StringComparison.OrdinalIgnoreCase)
            && IsPathUnderStandardsApp(currentPath))
            return true;

        return false;
    }
}
