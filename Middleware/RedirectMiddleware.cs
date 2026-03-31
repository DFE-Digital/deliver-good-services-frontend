using ServiceManual.Services;

namespace ServiceManual.Middleware;

/// <summary>
/// Global route processor: handles redirector short URLs by looking up the short URL in the CMS
/// and redirecting to the configured urlToRedirectTo.
/// </summary>
public class RedirectMiddleware
{
    private const string RedirectPathPrefix = "/redirect/";
    private const string LegacyRedirectPathPrefix = "/go/";
    private readonly RequestDelegate _next;
    private readonly ILogger<RedirectMiddleware> _logger;

    public RedirectMiddleware(RequestDelegate next, ILogger<RedirectMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var cms = context.RequestServices.GetService<ICmsApiService>();
        if (cms == null)
        {
            _logger.LogWarning("Redirect middleware: ICmsApiService not available");
            await _next(context);
            return;
        }

        var shortUrl = TryGetShortUrlFromPrefixedPath(path);
        if (!string.IsNullOrWhiteSpace(shortUrl))
        {
            await TryApplyRedirectAsync(context, cms, shortUrl, path, clear404Response: false);
            return;
        }

        await _next(context);

        if (context.Response.HasStarted || context.Response.StatusCode != StatusCodes.Status404NotFound)
            return;

        shortUrl = TryGetLegacyRootShortUrl(path, context.Request.Method);
        if (string.IsNullOrWhiteSpace(shortUrl))
            return;

        await TryApplyRedirectAsync(context, cms, shortUrl, path, clear404Response: true);
    }

    private static string? TryGetShortUrlFromPrefixedPath(string path)
    {
        var prefix = path.StartsWith(RedirectPathPrefix, StringComparison.OrdinalIgnoreCase)
            ? RedirectPathPrefix
            : path.StartsWith(LegacyRedirectPathPrefix, StringComparison.OrdinalIgnoreCase)
                ? LegacyRedirectPathPrefix
                : null;

        if (prefix == null)
            return null;

        var segment = path[prefix.Length..].TrimStart('/');
        var slashIndex = segment.IndexOf('/');
        return slashIndex >= 0 ? segment[..slashIndex] : segment;
    }

    private static string? TryGetLegacyRootShortUrl(string path, string method)
    {
        if (!HttpMethods.IsGet(method) && !HttpMethods.IsHead(method))
            return null;

        if (string.IsNullOrWhiteSpace(path) || path == "/")
            return null;

        var trimmedPath = path.Trim('/');
        if (string.IsNullOrWhiteSpace(trimmedPath) || trimmedPath.Contains('/') || trimmedPath.Contains('.'))
            return null;

        return trimmedPath;
    }

    private async Task TryApplyRedirectAsync(HttpContext context, ICmsApiService cms, string shortUrl, string requestedPath, bool clear404Response)
    {
        // Tell crawlers not to index or follow redirect URLs.
        context.Response.Headers["X-Robots-Tag"] = "noindex, nofollow";

        var redirectTo = await cms.GetRedirectUrlByShortUrlAsync(shortUrl);
        if (string.IsNullOrWhiteSpace(redirectTo))
        {
            return;
        }

        if (clear404Response)
        {
            context.Response.Clear();
        }

        // Allow absolute http(s) URLs or relative paths starting with /
        var isAbsolute = Uri.TryCreate(redirectTo, UriKind.Absolute, out var uri)
                         && uri != null
                         && (uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase)
                             || uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase));
        var isRelative = redirectTo.StartsWith("/", StringComparison.Ordinal) && !redirectTo.StartsWith("//", StringComparison.Ordinal);

        if (!isAbsolute && !isRelative)
        {
            _logger.LogWarning("Redirect middleware: invalid urlToRedirectTo for shortUrl '{ShortUrl}' (must be absolute http(s) or path starting with /)", shortUrl);
            return;
        }

        _logger.LogInformation("Redirect: {RequestedPath} -> {RedirectTo} (shortUrl: {ShortUrl})", requestedPath, redirectTo, shortUrl);
        context.Response.Redirect(redirectTo, permanent: false);
    }
}
