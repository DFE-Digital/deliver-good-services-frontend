using ServiceManual.Services;

namespace ServiceManual.Middleware;

/// <summary>
/// Checks the request path against CMS "301 Redirects". If a rule exists, either performs a 301
/// redirect to the new path or sends the user to the interim "content has moved" page.
/// </summary>
public class PathRedirectMiddleware
{
    private const string InterimPagePath = "/moved";
    private readonly RequestDelegate _next;
    private readonly ILogger<PathRedirectMiddleware> _logger;

    public PathRedirectMiddleware(RequestDelegate next, ILogger<PathRedirectMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var normalizedPath = path.Trim();
        if (!normalizedPath.StartsWith("/", StringComparison.Ordinal))
            normalizedPath = "/" + normalizedPath;

        // Do not apply path redirects to the interim page or short-URL redirects
        if (normalizedPath.StartsWith(InterimPagePath, StringComparison.OrdinalIgnoreCase) ||
            normalizedPath.StartsWith("/redirect/", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var cms = context.RequestServices.GetService<ICmsApiService>();
        if (cms == null)
        {
            _logger.LogWarning("PathRedirectMiddleware: ICmsApiService not available");
            await _next(context);
            return;
        }

        _logger.LogDebug("PathRedirectMiddleware: checking path '{Path}'", normalizedPath);
        var rule = await cms.GetPathRedirectByOldPathAsync(normalizedPath);
        if (rule == null)
        {
            await _next(context);
            return;
        }

        var newPath = rule.NewPath.Trim();
        var isAbsolute = Uri.TryCreate(newPath, UriKind.Absolute, out var uri)
                        && uri != null
                        && (uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase)
                            || uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase));
        var isRelative = newPath.StartsWith("/", StringComparison.Ordinal) && !newPath.StartsWith("//", StringComparison.Ordinal);

        if (!isAbsolute && !isRelative)
        {
            _logger.LogWarning("Path redirect: invalid newPath for oldPath '{OldPath}' (must be absolute http(s) or path starting with /)", rule.OldPath);
            await _next(context);
            return;
        }

        if (rule.UseInterimPage)
        {
            var toEncoded = Uri.EscapeDataString(newPath);
            var interimUrl = $"{InterimPagePath}?to={toEncoded}";
            _logger.LogInformation("Path redirect (interim): {OldPath} -> {InterimUrl}", rule.OldPath, interimUrl);
            context.Response.Redirect(interimUrl, permanent: false);
            return;
        }

        _logger.LogInformation("Path redirect (301): {OldPath} -> {NewPath}", rule.OldPath, newPath);
        context.Response.Redirect(newPath, permanent: true);
    }
}
