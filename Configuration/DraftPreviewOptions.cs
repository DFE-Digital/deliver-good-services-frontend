using Microsoft.Extensions.Configuration;

namespace ServiceManual.Configuration;

/// <summary>
/// When <see cref="Enabled"/> is true, the site requests Strapi draft content (<c>status=draft</c>) for all CMS calls.
/// Configure per environment / frontend (e.g. Test UI vs Production UI pointing at the same CMS).
/// </summary>
public sealed class DraftPreviewOptions
{
    public const string SectionName = "DraftPreview";

    public bool Enabled { get; set; }
}

/// <summary>
/// Reads <c>DraftPreview:Enabled</c> reliably from JSON and environment variables (<c>DraftPreview__Enabled</c>),
/// including values like <c>1</c> / <c>true</c> that <see cref="IConfiguration.GetValue{T}(string)"/> can mishandle as bool.
/// </summary>
public static class DraftPreviewConfigurationReader
{
    private const string EnabledKey = "DraftPreview:Enabled";

    public static bool IsDraftPreviewEnabled(this IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var raw = configuration[EnabledKey];
        if (string.IsNullOrWhiteSpace(raw))
            return false;
        if (bool.TryParse(raw, out var b))
            return b;
        var t = raw.Trim();
        return t == "1"
            || t.Equals("true", StringComparison.OrdinalIgnoreCase)
            || t.Equals("yes", StringComparison.OrdinalIgnoreCase);
    }
}
