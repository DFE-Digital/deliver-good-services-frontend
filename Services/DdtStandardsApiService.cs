using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ServiceManual.Services;

/// <summary>
/// Service for interacting with the Standards Strapi CMS (StandardsCMS).
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
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// Get published standards (supports search, categories, pagination) from the Standards CMS.
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
                "sort=title",
                $"pagination[page]={page}",
                $"pagination[pageSize]={pageSize}",
                "pagination[withCount]=true",
                "populate[categories]=true",
                "populate[sub_categories]=true"
            };

            if (!string.IsNullOrWhiteSpace(search))
                queryParams.Add($"filters[title][$containsi]={Uri.EscapeDataString(search.Trim())}");
            if (categories != null)
            {
                var catList = categories.Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
                for (var i = 0; i < catList.Count; i++)
                    queryParams.Add($"filters[categories][title][$in][{i}]={Uri.EscapeDataString(catList[i]!.Trim())}");
            }

            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                var dir = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";
                queryParams.Add($"sort[0]={Uri.EscapeDataString(sortBy!.Trim())}:{dir}");
            }

            var queryString = string.Join("&", queryParams);
            var url = $"api/standards?{queryString}";

            _logger.LogInformation("Fetching published standards from Standards CMS: {Url}", url);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var root = JsonNode.Parse(json);
            var dataArray = root?["data"] as JsonArray;
            var meta = root?["meta"];
            var pagination = meta?["pagination"];

            var list = new List<DdtStandardDto>();
            if (dataArray != null)
            {
                foreach (var item in dataArray)
                {
                    var dto = MapStrapiStandardToDto(item);
                    if (dto != null)
                        list.Add(dto);
                }
            }

            var total = pagination?["total"]?.GetValue<int>() ?? list.Count;
            var pageCount = pagination?["pageCount"]?.GetValue<int>() ?? 1;

            _logger.LogInformation("Successfully fetched {Count} standards from Standards CMS", list.Count);

            return new DdtStandardsResponse
            {
                Data = list,
                Pagination = new DdtStandardsPagination
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalPages = pageCount,
                    TotalRecords = total
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching published standards from Standards CMS");
            throw;
        }
    }

    /// <summary>
    /// Get a single published standard by numeric id. Looks up by slug if slug is parseable, otherwise returns null.
    /// </summary>
    public async Task<DdtStandardDetailDto?> GetStandardByIdAsync(int id)
    {
        try
        {
            var queryParams = $"filters[id][$eq]={id}&populate[categories][populate][sub_categories]=true&populate[sub_categories]=true&populate[phases]=true";
            var url = $"api/standards?{queryParams}";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Standards CMS returned {StatusCode} for standard id {Id}", response.StatusCode, id);
                return null;
            }
            var json = await response.Content.ReadAsStringAsync();
            var detail = ParseSingleStandardFromResponse(json);
            return detail;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error fetching standard by id {Id}", id);
            return null;
        }
    }

    /// <summary>
    /// Get a single published standard by slug from the Standards CMS.
    /// </summary>
    public async Task<DdtStandardDetailDto?> GetStandardBySlugAsync(string slug)
    {
        try
        {
            var queryParams = "filters[slug][$eq]=" + Uri.EscapeDataString(slug.Trim()) +
                "&populate[categories][populate][sub_categories]=true" +
                "&populate[sub_categories]=true" +
                "&populate[phases]=true";
            var url = $"api/standards?{queryParams}";

            _logger.LogInformation("Fetching standard with slug {Slug} from Standards CMS", slug);

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Standards CMS returned status {StatusCode} for slug {Slug}. Response: {Content}",
                    response.StatusCode, slug, errorContent.Length > 500 ? errorContent.Substring(0, 500) : errorContent);
                return null;
            }

            var contentType = response.Content.Headers.ContentType?.MediaType;
            if (contentType != null && !contentType.Contains("json"))
            {
                _logger.LogWarning("Standards CMS returned non-JSON content type {ContentType} for slug {Slug}", contentType, slug);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json) || (!json.TrimStart().StartsWith("{") && !json.TrimStart().StartsWith("[")))
            {
                _logger.LogWarning("Standards CMS returned non-JSON response for slug {Slug}", slug);
                return null;
            }

            return ParseSingleStandardFromResponse(json);
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

    /// <summary>
    /// Base URL for the standards source (e.g. link to view standard in CMS or external site).
    /// </summary>
    public string GetCompassBaseUrl()
    {
        return _configuration["StandardsCMS:BaseUrl"]?.TrimEnd('/') ?? _configuration["CompassApi:BaseUrl"] ?? "https://compass.education.gov.uk";
    }

    /// <summary>
    /// URL for the Create and manage standards admin (no trailing slash). Returns null if not configured.
    /// </summary>
    public string? GetManageStandardsBaseUrl()
    {
        return _configuration["ManageStandards:Url"]?.TrimEnd('/');
    }

    /// <summary>
    /// Full URL to manage a standard in the admin: ManageStandards:Url + "/" + documentId.
    /// </summary>
    public string? GetManageStandardUrl(string? documentId)
    {
        var baseUrl = GetManageStandardsBaseUrl();
        if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(documentId)) return null;
        return $"{baseUrl}/{Uri.EscapeDataString(documentId)}";
    }

    private static DdtStandardDto? MapStrapiStandardToDto(JsonNode? node)
    {
        if (node == null) return null;
        var attrs = node["attributes"] ?? node;
        var id = node["id"]?.GetValue<int>() ?? 0;
        var title = GetString(attrs, "title");
        var slug = GetString(attrs, "slug");
        if (string.IsNullOrEmpty(slug) && string.IsNullOrEmpty(title)) return null;

        var catTitles = GetRelationTitles(node["categories"] ?? attrs["categories"] ?? attrs["category"], "title");
        var subCatTitles = GetRelationTitles(node["sub_categories"] ?? node["subCategories"] ?? attrs["sub_categories"] ?? attrs["subCategories"], "title");

        return new DdtStandardDto
        {
            Id = id,
            Title = title,
            Slug = slug ?? "",
            Summary = GetString(attrs, "summary"),
            Categories = catTitles,
            SubCategories = subCatTitles,
            IsPublished = true
        };
    }

    private static DdtStandardDetailDto? ParseSingleStandardFromResponse(string json)
    {
        var root = JsonNode.Parse(json);
        var dataArray = root?["data"] as JsonArray;
        var item = dataArray?.FirstOrDefault();
        if (item == null) return null;
        return MapStrapiStandardToDetailDto(item);
    }

    private static DdtStandardDetailDto? MapStrapiStandardToDetailDto(JsonNode? node)
    {
        if (node == null) return null;
        var attrs = node["attributes"] ?? node;
        var id = node["id"]?.GetValue<int>() ?? 0;
        var title = GetString(attrs, "title");
        var slug = GetString(attrs, "slug");
        if (string.IsNullOrEmpty(slug) && string.IsNullOrEmpty(title)) return null;

        var categories = new List<DdtStandardCategoryDto>();
        var catNodes = GetRelationArray(node["categories"] ?? attrs["categories"] ?? attrs["category"]);
        foreach (var c in catNodes)
        {
            if (c == null) continue;
            var catAttrs = c["attributes"] ?? c;
            var catId = c["id"]?.GetValue<int>() ?? 0;
            var catName = GetString(catAttrs, "title");
            if (string.IsNullOrEmpty(catName)) continue;
            var subCats = new List<DdtStandardSubCategoryDto>();
            var subNodes = GetRelationArray(catAttrs?["sub_categories"] ?? catAttrs?["subCategories"]);
            foreach (var sc in subNodes)
            {
                if (sc == null) continue;
                var scAttrs = sc["attributes"] ?? sc;
                subCats.Add(new DdtStandardSubCategoryDto
                {
                    Id = sc["id"]?.GetValue<int>() ?? 0,
                    Name = GetString(scAttrs, "title"),
                    Description = GetString(scAttrs, "description")
                });
            }
            categories.Add(new DdtStandardCategoryDto
            {
                Id = catId,
                Name = catName,
                Description = GetString(catAttrs, "description"),
                SubCategories = subCats
            });
        }

        var subCategoriesDirect = GetRelationArray(node["sub_categories"] ?? node["subCategories"] ?? attrs["sub_categories"] ?? attrs["subCategories"]);
        foreach (var sc in subCategoriesDirect)
        {
            if (sc == null) continue;
            var scAttrs = sc["attributes"] ?? sc;
            var scName = GetString(scAttrs, "title");
            if (string.IsNullOrEmpty(scName)) continue;
            var alreadyIn = categories.Any(c => c.SubCategories.Any(s => string.Equals(s.Name, scName, StringComparison.OrdinalIgnoreCase)));
            if (!alreadyIn)
            {
                var firstCat = categories.FirstOrDefault();
                if (firstCat != null)
                    firstCat.SubCategories.Add(new DdtStandardSubCategoryDto { Id = sc["id"]?.GetValue<int>() ?? 0, Name = scName });
                else
                    categories.Add(new DdtStandardCategoryDto { Name = "Other", SubCategories = new List<DdtStandardSubCategoryDto> { new DdtStandardSubCategoryDto { Name = scName } } });
            }
        }

        var phases = new List<DdtStandardPhase>();
        var phaseNodes = GetRelationArray(node["phases"] ?? attrs["phases"]);
        foreach (var p in phaseNodes)
        {
            if (p == null) continue;
            var pAttrs = p["attributes"] ?? p;
            var name = GetString(pAttrs, "Title") ?? GetString(pAttrs, "title");
            if (!string.IsNullOrEmpty(name))
                phases.Add(new DdtStandardPhase { Id = p["id"]?.GetValue<int>() ?? 0, Name = name });
        }

        var documentId = GetString(node, "documentId") ?? GetString(attrs, "documentId");

        return new DdtStandardDetailDto
        {
            Id = id,
            DocumentId = documentId,
            Title = title,
            Slug = slug ?? "",
            Summary = GetString(attrs, "summary"),
            Purpose = GetString(attrs, "purpose"),
            HowToMeet = GetString(attrs, "howToMeet"),
            Governance = GetString(attrs, "governance"),
            RelatedGuidance = GetString(attrs, "relatedGuidance"),
            LegalBasis = GetString(attrs, "legalBasis"),
            LegalStandard = GetBool(attrs, "legalStandard"),
            Categories = categories,
            Phases = phases,
            IsPublished = true
        };
    }

    private static string? GetString(JsonNode? node, string key)
    {
        if (node == null) return null;
        var v = node[key];
        if (v == null) return null;
        if (v is JsonValue jv && jv.TryGetValue(out string? s)) return s;
        return v.GetValue<string>();
    }

    private static bool? GetBool(JsonNode? node, string key)
    {
        if (node == null) return null;
        var v = node[key];
        if (v == null) return null;
        if (v is JsonValue jv && jv.TryGetValue(out bool b)) return b;
        return null;
    }

    private static JsonArray GetRelationArray(JsonNode? node)
    {
        if (node == null) return new JsonArray();
        if (node is JsonArray arr) return arr;
        var data = node["data"];
        if (data is JsonArray dataArr) return dataArr;
        if (data != null) return new JsonArray { data };
        return new JsonArray();
    }

    private static List<string> GetRelationTitles(JsonNode? node, string titleKey)
    {
        var list = new List<string>();
        var arr = GetRelationArray(node);
        foreach (var item in arr)
        {
            var attrs = item?["attributes"] ?? item;
            var t = GetString(attrs, titleKey);
            if (!string.IsNullOrEmpty(t)) list.Add(t);
        }
        return list;
    }
}

// ── API response and DTOs (unchanged for compatibility with views and controller) ───────────────────────────────────────────────────

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
    /// <summary>Strapi documentId (e.g. for linking to Create and manage standards admin).</summary>
    public string? DocumentId { get; set; }
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
