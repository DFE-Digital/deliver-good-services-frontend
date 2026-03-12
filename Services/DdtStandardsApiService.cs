using System.Net.Http.Headers;
using System.Text.Json;

namespace ServiceManual.Services;

/// <summary>
/// Service for interacting with the COMPASS DDT Standards API.
/// </summary>
public class DdtStandardsApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DdtStandardsApiService> _logger;
    private readonly IConfiguration _configuration;
    private readonly JsonSerializerOptions _jsonOptions;

    public DdtStandardsApiService(
        HttpClient httpClient,
        ILogger<DdtStandardsApiService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>
    /// Get published standards by stage (supports search, categories, pagination).
    /// </summary>
    public async Task<DdtStandardsResponse?> GetPublishedStandardsAsync(
        string? search = null,
        IEnumerable<string>? categories = null,
        string? sortBy = null,
        string? sortDirection = null,
        int page = 1,
        int pageSize = 50)
    {
        try
        {
            var queryParams = new List<string>
            {
                "stage=published",
                $"page={page}",
                $"pageSize={pageSize}"
            };

            if (!string.IsNullOrWhiteSpace(search))
                queryParams.Add($"search={Uri.EscapeDataString(search)}");
            if (categories != null)
            {
                foreach (var c in categories.Where(c => !string.IsNullOrWhiteSpace(c)))
                    queryParams.Add($"category={Uri.EscapeDataString(c!)}");
            }
            if (!string.IsNullOrWhiteSpace(sortBy))
                queryParams.Add($"sortBy={Uri.EscapeDataString(sortBy)}");
            if (!string.IsNullOrWhiteSpace(sortDirection))
                queryParams.Add($"sortDirection={Uri.EscapeDataString(sortDirection)}");

            var queryString = string.Join("&", queryParams);
            var url = $"/api/v1/DdtStandards/by-stage?{queryString}";

            _logger.LogInformation("Fetching published standards from {Url}", url);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<DdtStandardsResponse>(json, _jsonOptions);

            _logger.LogInformation("Successfully fetched {Count} standards", result?.Data?.Count ?? 0);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching published standards");
            throw;
        }
    }

    /// <summary>
    /// Get a single published standard by numeric id (e.g. 301).
    /// Calls GET /api/v1/DdtStandards/by-id/{id}. Returns null if not found or API does not support by-id.
    /// </summary>
    public async Task<DdtStandardDetailDto?> GetStandardByIdAsync(int id)
    {
        try
        {
            var url = $"/api/v1/DdtStandards/by-id/{id}";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("API returned {StatusCode} for standard id {Id}", response.StatusCode, id);
                return null;
            }
            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json) || (!json.TrimStart().StartsWith("{") && !json.TrimStart().StartsWith("[")))
                return null;
            return JsonSerializer.Deserialize<DdtStandardDetailDto>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error fetching standard by id {Id}", id);
            return null;
        }
    }

    /// <summary>
    /// Get a single published standard by slug.
    /// </summary>
    public async Task<DdtStandardDetailDto?> GetStandardBySlugAsync(string slug)
    {
        try
        {
            var url = $"/api/v1/DdtStandards/by-slug/{Uri.EscapeDataString(slug)}";

            _logger.LogInformation("Fetching standard with slug {Slug} from {Url}", slug, url);

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("API returned status {StatusCode} for standard slug {Slug}. Response: {Content}",
                    response.StatusCode, slug, errorContent.Length > 500 ? errorContent.Substring(0, 500) : errorContent);
                return null;
            }

            var contentType = response.Content.Headers.ContentType?.MediaType;
            if (contentType != null && !contentType.Contains("json"))
            {
                _logger.LogWarning("API returned non-JSON content type {ContentType} for standard slug {Slug}", contentType, slug);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json) || (!json.TrimStart().StartsWith("{") && !json.TrimStart().StartsWith("[")))
            {
                _logger.LogWarning("API returned non-JSON response for standard slug {Slug}", slug);
                return null;
            }

            return JsonSerializer.Deserialize<DdtStandardDetailDto>(json, _jsonOptions);
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "JSON deserialization error for standard slug {Slug}", slug);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching standard with slug {Slug}", slug);
            throw;
        }
    }

    public string GetCompassBaseUrl()
    {
        return _configuration["CompassApi:BaseUrl"] ?? "https://compass.education.gov.uk";
    }
}

// ── API response and DTOs ───────────────────────────────────────────────────

public class DdtStandardsResponse
{
    public List<DdtStandardDto> Data { get; set; } = [];
    public DdtStandardsPagination? Pagination { get; set; }
    public string? Stage { get; set; }
}

public class DdtStandardsPagination
{
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public int TotalRecords { get; set; }
}

public class DdtStandardDto
{
    public int Id { get; set; }
    public string? StandardUuid { get; set; }
    public string? LegacyId { get; set; }
    public string? Title { get; set; }
    public string? Slug { get; set; }
    public string? Summary { get; set; }
    public string? Version { get; set; }
    public string? Stage { get; set; }
    public bool IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? FirstPublished { get; set; }
    public DateTime? LastUpdated { get; set; }
    public List<string> Categories { get; set; } = [];
    public List<string> SubCategories { get; set; } = [];
    public List<DdtStandardPhase> Phases { get; set; } = [];
}

public class DdtStandardPhase
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

public class DdtStandardDetailDto
{
    public int Id { get; set; }
    public string? StandardUuid { get; set; }
    public string? LegacyId { get; set; }
    public string? Title { get; set; }
    public string? Slug { get; set; }
    public string? Summary { get; set; }
    public string? Purpose { get; set; }
    public string? HowToMeet { get; set; }
    public string? Governance { get; set; }
    public string? Version { get; set; }
    public string? Stage { get; set; }
    public bool IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? FirstPublished { get; set; }
    public DateTime? LastUpdated { get; set; }
    public string? RelatedGuidance { get; set; }
    public string? LegalBasis { get; set; }
    public bool? LegalStandard { get; set; }
    public List<DdtStandardCategoryDto> Categories { get; set; } = [];
    public List<DdtStandardPhase> Phases { get; set; } = [];
}

public class DdtStandardCategoryDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<DdtStandardSubCategoryDto> SubCategories { get; set; } = [];
}

public class DdtStandardSubCategoryDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
}
