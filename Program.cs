using Microsoft.Extensions.Configuration;
using ServiceManual.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to use port 5066
builder.WebHost.UseUrls("http://localhost:5066");

// Add services to the container
builder.Services.Configure<DraftPreviewOptions>(builder.Configuration.GetSection(DraftPreviewOptions.SectionName));
builder.Services.AddMemoryCache();
builder.Services.AddTransient<ServiceManual.Services.CmsDraftPreviewRequestHeadersHandler>();
builder.Services.AddHttpClient<ServiceManual.Services.ICmsApiService, ServiceManual.Services.CmsApiService>()
    .AddHttpMessageHandler<ServiceManual.Services.CmsDraftPreviewRequestHeadersHandler>();
// Same CMS base URL/token but no draft header — used to check whether a published Strapi row exists (banner logic).
builder.Services.AddHttpClient("CmsApiPublished", (sp, client) =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var baseUrl = configuration["CmsApi:BaseUrl"] ?? string.Empty;
    var apiToken = configuration["CmsApi:ApiToken"] ?? string.Empty;
    if (!string.IsNullOrEmpty(baseUrl))
        client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    if (!string.IsNullOrEmpty(apiToken))
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiToken);
});
builder.Services.AddHttpClient<ServiceManual.Services.DdtStandardsApiService>((sp, client) =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var baseUrl = configuration["StandardsCMS:BaseUrl"] ?? "https://dfe-standards-cms-217ce4e280a0.herokuapp.com/";
    var apiToken = configuration["StandardsCMS:ApiToken"] ?? "";
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    if (!string.IsNullOrEmpty(apiToken))
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiToken);
});
builder.Services.AddScoped<ServiceManual.Services.INavigationService, ServiceManual.Services.NavigationService>();
builder.Services.AddScoped<ServiceManual.Services.ISearchService, ServiceManual.Services.SearchService>();
builder.Services.AddSingleton<ServiceManual.Services.IBlobMetadataProvider, ServiceManual.Services.AzureBlobMetadataProvider>();
builder.Services.AddScoped<ServiceManual.Filters.NavigationFilter>();
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.AddService<ServiceManual.Filters.NavigationFilter>();
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

// Path-based 301 redirects (old path -> new path or interim "content has moved" page)
app.UseMiddleware<ServiceManual.Middleware.PathRedirectMiddleware>();

// Global redirect: /redirect/{shorturl} -> lookup in CMS, redirect to urlToRedirectTo
app.UseMiddleware<ServiceManual.Middleware.RedirectMiddleware>();

app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

