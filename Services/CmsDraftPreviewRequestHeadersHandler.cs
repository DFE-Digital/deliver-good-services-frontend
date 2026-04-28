using System.Net.Http;
using Microsoft.Extensions.Configuration;
using ServiceManual.Configuration;

namespace ServiceManual.Services;

/// <summary>
/// Adds <c>X-Cms-Draft-Preview: 1</c> to outgoing Strapi requests when <c>DraftPreview:Enabled</c> is true.
/// Uses <see cref="IConfiguration"/> so deployment env vars (e.g. <c>DraftPreview__Enabled</c>) apply the same as <c>CmsApiService</c>.
/// </summary>
public sealed class CmsDraftPreviewRequestHeadersHandler : DelegatingHandler
{
    private readonly IConfiguration _configuration;

    public CmsDraftPreviewRequestHeadersHandler(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (_configuration.IsDraftPreviewEnabled())
        {
            request.Headers.Remove("X-Cms-Draft-Preview");
            request.Headers.TryAddWithoutValidation("X-Cms-Draft-Preview", "1");
        }

        return base.SendAsync(request, cancellationToken);
    }
}
