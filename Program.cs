var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to use port 5066
builder.WebHost.UseUrls("http://localhost:5066");

// Add services to the container
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<ServiceManual.Services.ICmsApiService, ServiceManual.Services.CmsApiService>();
builder.Services.AddHttpClient<ServiceManual.Services.DdtStandardsApiService>((sp, client) =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var baseUrl = configuration["CompassApi:BaseUrl"] ?? "https://compass.education.gov.uk";
    var apiKey = configuration["CompassApi:ApiKey"] ?? configuration["CompassApi:AccessToken"] ?? "";
    client.BaseAddress = new Uri(baseUrl);
    if (!string.IsNullOrEmpty(apiKey))
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
});
builder.Services.AddScoped<ServiceManual.Services.INavigationService, ServiceManual.Services.NavigationService>();
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

// Global redirect: /redirect/{shorturl} -> lookup in CMS, redirect to urlToRedirectTo
app.UseMiddleware<ServiceManual.Middleware.RedirectMiddleware>();

app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

