using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using ServiceManual.Models;

namespace ServiceManual.Services
{
    public class CmsApiService : ICmsApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CmsApiService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public CmsApiService(HttpClient httpClient, IConfiguration configuration, ILogger<CmsApiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            var baseUrl = configuration["CmsApi:BaseUrl"] ?? string.Empty;
            var apiToken = configuration["CmsApi:ApiToken"] ?? string.Empty;

            if (!string.IsNullOrEmpty(baseUrl))
                _httpClient.BaseAddress = new Uri(baseUrl);

            if (!string.IsNullOrEmpty(apiToken))
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
        }

        private string? GetCmsBaseUrl()
        {
            var baseUrl = _httpClient.BaseAddress?.ToString();
            return string.IsNullOrEmpty(baseUrl) ? null : baseUrl.TrimEnd('/');
        }

        public async Task<GuidanceIndexPage?> GetGuidanceIndexAsync()
        {
            try
            {
                const string url = "api/guidance-areas/index";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("CMS API returned {StatusCode} for guidance index", response.StatusCode);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<StrapiCollectionResponse<StrapiGuidanceArea>>(json, JsonOptions);

                return new GuidanceIndexPage
                {
                    Areas = result?.Data?
                        .Where(area => !string.IsNullOrWhiteSpace(area.Slug) && !string.IsNullOrWhiteSpace(area.Name))
                        .Select(area => new GuidanceAreaGroup
                        {
                            Name = area.Name ?? string.Empty,
                            Slug = area.Slug ?? string.Empty,
                            Summary = string.IsNullOrWhiteSpace(area.Summary) ? null : area.Summary.Trim(),
                            Description = string.IsNullOrWhiteSpace(area.Description) ? null : area.Description.Trim(),
                            ColourHex = NormaliseHex(area.ColourHex),
                            FeaturedProfessions = (area.FeaturedProfessions ?? [])
                                .Where(tag => !string.IsNullOrWhiteSpace(tag.Slug) || !string.IsNullOrWhiteSpace(tag.Title))
                                .Select(tag => new TagRef
                                {
                                    Slug = CoalesceSlug(tag.Slug, tag.Title),
                                    Title = tag.Title ?? string.Empty
                                })
                                .ToList(),
                            Collections = (area.Collections ?? [])
                                .Where(collection => !string.IsNullOrWhiteSpace(collection.Slug) && !string.IsNullOrWhiteSpace(collection.Title))
                                .Select(collection => new GuidanceCollectionCard
                                {
                                    Title = collection.Title ?? string.Empty,
                                    Slug = collection.Slug ?? string.Empty,
                                    Url = string.IsNullOrWhiteSpace(collection.Url)
                                        ? $"/guidance/collections/{collection.Slug}"
                                        : collection.Url.Trim(),
                                    ContentType = string.IsNullOrWhiteSpace(collection.ContentType)
                                        ? "Collection"
                                        : collection.ContentType.Trim(),
                                    Description = string.IsNullOrWhiteSpace(collection.Description)
                                        ? collection.Title ?? string.Empty
                                        : collection.Description.Trim(),
                                    ItemCount = collection.ItemCount,
                                    Featured = collection.Featured,
                                    Tags = (collection.Tags ?? [])
                                        .Where(tag => !string.IsNullOrWhiteSpace(tag))
                                        .Select(tag => tag.Trim())
                                        .ToList(),
                                    ApplicableProfessions = (collection.ApplicableProfessions ?? [])
                                        .Where(tag => !string.IsNullOrWhiteSpace(tag.Slug) || !string.IsNullOrWhiteSpace(tag.Title))
                                        .Select(tag => new TagRef
                                        {
                                            Slug = CoalesceSlug(tag.Slug, tag.Title),
                                            Title = tag.Title ?? string.Empty
                                        })
                                        .ToList(),
                                    AlsoInAreas = (collection.AlsoInAreas ?? [])
                                        .Where(areaRef => !string.IsNullOrWhiteSpace(areaRef.Slug) && !string.IsNullOrWhiteSpace(areaRef.Title))
                                        .Select(areaRef => new CollectionRef { Slug = areaRef.Slug ?? string.Empty, Title = areaRef.Title ?? string.Empty })
                                        .ToList()
                                })
                                .ToList()
                        })
                        .ToList() ?? []
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching guidance index");
                return null;
            }
        }

        public async Task<List<ArticleSummary>> GetArticlesAsync()
        {
            var items = await FetchArticlesAsync();
            var cmsBaseUrl = GetCmsBaseUrl();
            return items
                .Select(a => new { Item = a, RouteKey = ResolveArticleRouteKey(a.Slug, a.DocumentId, a.Id) })
                .Where(x => !string.IsNullOrEmpty(x.RouteKey))
                .Select(x => new ArticleSummary
                {
                    RouteKey = x.RouteKey!,
                    Title = x.Item.Title ?? string.Empty,
                    MetaDescription = x.Item.MetaDescription,
                    Author = string.IsNullOrWhiteSpace(x.Item.Author) ? null : x.Item.Author.Trim(),
                    PublishedFromDisplay = FormatDateTime(x.Item.PublishedFrom),
                    LeadImageUrl = ResolveArticleLeadImageUrl(x.Item, cmsBaseUrl),
                    LeadImageAlt = ResolveArticleLeadImageAlt(x.Item)
                })
                .ToList();
        }

        public async Task<Article?> GetArticleByRouteKeyAsync(string routeKey)
        {
            if (string.IsNullOrWhiteSpace(routeKey))
                return null;

            try
            {
                var url = $"api/articles/by-slug/{Uri.EscapeDataString(routeKey.Trim())}" +
                          "?populate[leadImage][fields][0]=url&populate[leadImage][fields][1]=alternativeText";
                var response = await _httpClient.GetAsync(url);

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("CMS API returned {StatusCode} for article slug '{Slug}'", response.StatusCode, routeKey);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<StrapiCollectionResponse<StrapiArticle>>(json, JsonOptions);
                var item = result?.Data?.FirstOrDefault();

                if (item is null)
                    return null;

                var cmsBaseUrl = GetCmsBaseUrl();

                return new Article
                {
                    RouteKey = item.Slug ?? item.DocumentId ?? item.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Title = item.Title ?? string.Empty,
                    MetaDescription = item.MetaDescription,
                    Body = item.Body,
                    Author = string.IsNullOrWhiteSpace(item.Author) ? null : item.Author.Trim(),
                    PublishedFromDisplay = FormatDateTime(item.PublishedFrom),
                    PublishedToDisplay = FormatDateTime(item.PublishedTo),
                    LeadImageUrl = ResolveArticleLeadImageUrl(item, cmsBaseUrl),
                    LeadImageAlt = ResolveArticleLeadImageAlt(item)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching article for slug '{Slug}'", routeKey);
                return null;
            }
        }

        private async Task<List<StrapiArticle>> FetchArticlesAsync()
        {
            try
            {
                const string url = "api/articles" +
                                   "?publicationState=live" +
                                   "&pagination[pageSize]=200" +
                                   "&sort[0]=publishedFrom:desc&sort[1]=title:asc" +
                                   "&fields[0]=title&fields[1]=slug&fields[2]=metaDescription&fields[3]=author&fields[4]=publishedFrom" +
                                   "&populate[leadImage][fields][0]=url&populate[leadImage][fields][1]=alternativeText";

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("CMS API returned {StatusCode} for articles", response.StatusCode);
                    return [];
                }

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<StrapiCollectionResponse<StrapiArticle>>(json, JsonOptions);
                return result?.Data ?? [];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching articles");
                return [];
            }
        }

        public async Task<Collection?> GetCollectionBySlugAsync(string slug)
        {
            try
            {
                // Custom Strapi endpoint returns collection by slug with section items fully populated.
                var url = $"api/collections/by-slug/{Uri.EscapeDataString(slug)}";

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("CMS API returned {StatusCode} for collection slug '{Slug}'", response.StatusCode, slug);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<StrapiCollectionResponse<StrapiCollection>>(json, JsonOptions);

                var item = result?.Data?.FirstOrDefault();
                if (item is null)
                    return null;

                return new Collection
                {
                    Title = item.Title ?? string.Empty,
                    MetaDescription = item.MetaDescription ?? string.Empty,
                    Slug = item.Slug ?? string.Empty,
                    Body = item.Body,
                    ShowLastReviewedDateOnPage = item.ShowLastReviewedDateOnPage ?? false,
                    LastReviewedDateDisplay = FormatDateTime(item.LastReviewedDate),
                    Owner = item.ContentOwner?.Title?.Trim(),
                    OwnerUrl = item.ContentOwner?.RedirectUrl?.Trim(),
                    AudienceTags = item.ApplicableProfessions?
                        .Where(p => !string.IsNullOrWhiteSpace(p.Slug) || !string.IsNullOrWhiteSpace(p.Title))
                        .Select(p => new TagRef { Slug = p.Slug ?? "", Title = p.Title ?? "" })
                        .ToList() ?? [],
                    RelatedFiles = MapRelatedFiles(item.RelatedFiles, GetCmsBaseUrl()),
                    Sections = item.Collection_Sections?
                        .OrderBy(s => s.Order)
                        .Select(s => new CollectionSection
                        {
                            Title = s.Title ?? string.Empty,
                            Summary = s.Summary,
                            Items = (s.Items ?? [])
                                .Select(i => new ContentLink
                                {
                                    Title = i.Title ?? string.Empty,
                                    Slug = i.Slug,
                                    MetaDescription = i.MetaDescription,
                                    Url = i.Url ?? string.Empty,
                                    OpenInNewTab = i.NewTab,
                                    ExternalLink = i.ExternalLink,
                                    LinkType = i.LinkType,
                                    PriorityInGroup = i.PriorityInGroup,
                                    ContentType = ContentTypeLabel(i.Type),
                                    Grade = i.Grade
                                })
                                .OrderByDescending(i => i.PriorityInGroup)
                                .ToList()
                        })
                        .ToList() ?? [],
                    RelatedContent = item.RelatedContent?
                        .Select(r => new RelatedContentItem { Header = r.Header ?? string.Empty, Content = r.Content })
                        .ToList() ?? []
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching collection for slug '{Slug}'", slug);
                return null;
            }
        }

        public async Task<JobSpecification?> GetJobSpecificationBySlugAsync(string slug)
        {
            try
            {
                var url = $"api/job-specifications/by-slug/{Uri.EscapeDataString(slug)}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("CMS API returned {StatusCode} for job specification slug '{Slug}'", response.StatusCode, slug);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<StrapiCollectionResponse<StrapiJobSpecification>>(json, JsonOptions);

                var item = result?.Data?.FirstOrDefault();
                if (item is null)
                    return null;

                return new JobSpecification
                {
                    Title = item.Title ?? string.Empty,
                    Slug = item.Slug ?? string.Empty,
                    Grade = item.Grade,
                    RoleDescription = item.RoleDescription,
                    Skills = item.Skills,
                    EnableWordDocDownload = item.EnableWordDocDownload,
                    Profession = item.Profession != null
                        ? new JobSpecificationProfession
                        {
                            Title = item.Profession.Title,
                            Slug = item.Profession.Slug,
                            Plural = item.Profession.Plural,
                            ProfessionDescription = item.Profession.ProfessionDescription
                        }
                        : null,
                    SiblingJobSpecifications = (item.SiblingJobSpecifications ?? [])
                        .Select(s => new JobSpecificationSibling
                        {
                            Title = s.Title ?? string.Empty,
                            Slug = s.Slug ?? string.Empty,
                            Grade = s.Grade
                        })
                        .ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching job specification for slug '{Slug}'", slug);
                return null;
            }
        }

        public async Task<DetailedGuide?> GetDetailedGuideBySlugAsync(string slug)
        {
            try
            {
                var url = $"api/detailed-guides?filters[slug][$eq]={Uri.EscapeDataString(slug)}" +
                          "&fields[0]=title&fields[1]=slug&fields[2]=metaDescription&fields[3]=body&fields[4]=showLastReviewedDateOnPage&fields[5]=lastReviewedDate&fields[6]=hideContentsOnPrimaryPage&fields[7]=showOwnerOnPage&fields[8]=showApplicablePhasesOnPage&fields[9]=showApplicableProfessionsOnPage&fields[10]=customJS&fields[11]=customCSS&fields[12]=overrideOverviewTitle&fields[13]=showGuidePagesOnRight" +
                          "&populate[detailed_guide_pages][fields][0]=title&populate[detailed_guide_pages][fields][1]=slug&populate[detailed_guide_pages][fields][2]=metaDescription" +
                          "&populate[detailed_guide_pages][populate][applicablePhases][fields][0]=title&populate[detailed_guide_pages][populate][applicablePhases][fields][1]=slug" +
                          "&populate[detailed_guide_pages][populate][applicableProfessions][fields][0]=title&populate[detailed_guide_pages][populate][applicableProfessions][fields][1]=slug&populate[detailed_guide_pages][populate][applicableProfessions][fields][2]=plural" +
                          "&populate[collection][fields][0]=title&populate[collection][fields][1]=slug" +
                          "&populate[contentOwner][fields][0]=title&populate[contentOwner][populate][informationPage][fields][0]=urlToRedirectTo" +
                          "&populate[relatedContent][fields][0]=Header&populate[relatedContent][fields][1]=Content" +
                          "&populate[applicablePhases][fields][0]=title&populate[applicablePhases][fields][1]=slug" +
                          "&populate[applicableProfessions][fields][0]=title&populate[applicableProfessions][fields][1]=slug&populate[applicableProfessions][fields][2]=plural" +
                          "&populate[relatedFiles]=true";

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("CMS API returned {StatusCode} for detailed guide slug '{Slug}'", response.StatusCode, slug);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<StrapiCollectionResponse<StrapiDetailedGuide>>(json, JsonOptions);

                var item = result?.Data?.FirstOrDefault();
                if (item is null)
                    return null;

                var collections = item.Collection != null && !string.IsNullOrEmpty(item.Collection.Slug) && !string.IsNullOrEmpty(item.Collection.Title)
                    ? new List<CollectionRef> { new CollectionRef { Slug = item.Collection.Slug!, Title = item.Collection.Title! } }
                    : new List<CollectionRef>();
                var firstCollection = collections.FirstOrDefault();
                return new DetailedGuide
                {
                    Title = item.Title ?? string.Empty,
                    Slug = item.Slug ?? string.Empty,
                    MetaDescription = item.MetaDescription,
                    Body = item.Body,
                    OverrideOverviewTitle = item.OverrideOverviewTitle,
                    CollectionTitle = firstCollection?.Title,
                    CollectionSlug = firstCollection?.Slug,
                    Collections = collections,
                    HideContentsOnPrimaryPage = item.HideContentsOnPrimaryPage ?? false,
                    ShowGuidePagesOnRight = item.ShowGuidePagesOnRight ?? false,
                    Pages = item.Detailed_Guide_Pages?
                        .Select(p => new DetailedGuidePageSummary
                        {
                            Title = p.Title ?? string.Empty,
                            Slug = p.Slug ?? string.Empty,
                            MetaDescription = p.MetaDescription,
                            Phases = p.ApplicablePhases?
                                .Where(ph => !string.IsNullOrWhiteSpace(ph.Slug) || !string.IsNullOrWhiteSpace(ph.Title))
                                .Select(ph => new TagRef { Slug = ph.Slug ?? "", Title = ph.Title ?? "" })
                                .ToList() ?? [],
                            Professions = p.ApplicableProfessions?
                                .Where(pr => !string.IsNullOrWhiteSpace(pr.Slug) || !string.IsNullOrWhiteSpace(pr.Plural) || !string.IsNullOrWhiteSpace(pr.Title))
                                .Select(pr => new TagRef { Slug = pr.Slug ?? "", Title = (pr.Plural ?? pr.Title ?? "").Trim() })
                                .ToList() ?? [],
                        })
                        .ToList() ?? [],
                    RelatedContent = item.RelatedContent?
                        .Select(r => new RelatedContentItem { Header = r.Header ?? string.Empty, Content = r.Content })
                        .ToList() ?? [],
                    ShowLastReviewedDateOnPage = item.ShowLastReviewedDateOnPage ?? false,
                    LastReviewedDateDisplay = FormatDateTime(item.LastReviewedDate),
                    ShowOwnerOnPage = item.ShowOwnerOnPage ?? true,
                    Owner = item.ContentOwner?.Title?.Trim(),
                    OwnerUrl = item.ContentOwner?.RedirectUrl?.Trim(),
                    ShowApplicablePhasesOnPage = item.ShowApplicablePhasesOnPage ?? false,
                    PhaseTags = item.ApplicablePhases?
                        .Where(p => !string.IsNullOrWhiteSpace(p.Slug) || !string.IsNullOrWhiteSpace(p.Title))
                        .Select(p => new TagRef { Slug = p.Slug ?? "", Title = p.Title ?? "" })
                        .ToList() ?? [],
                    ShowApplicableProfessionsOnPage = item.ShowApplicableProfessionsOnPage ?? false,
                    Audience = item.ApplicableProfessions?
                        .Select(p => (p.Plural ?? p.Title ?? "").Trim())
                        .Where(p => !string.IsNullOrWhiteSpace(p))
                        .ToList() ?? [],
                    AudienceTags = item.ApplicableProfessions?
                        .Where(p => !string.IsNullOrWhiteSpace(p.Slug) || !string.IsNullOrWhiteSpace(p.Plural) || !string.IsNullOrWhiteSpace(p.Title))
                        .Select(p => new TagRef { Slug = p.Slug ?? "", Title = (p.Plural ?? p.Title ?? "").Trim() })
                        .ToList() ?? [],
                    RelatedFiles = MapRelatedFiles(item.RelatedFiles, GetCmsBaseUrl()),
                    CustomCSS = item.CustomCss,
                    CustomJS = item.CustomJs,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching detailed guide for slug '{Slug}'", slug);
                return null;
            }
        }

        public async Task<DetailedGuidePage?> GetDetailedGuidePageBySlugAsync(string guideSlug, string pageSlug)
        {
            try
            {
                var url = $"api/detailed-guide-pages?filters[slug][$eq]={Uri.EscapeDataString(pageSlug)}" +
                          "&fields[0]=title&fields[1]=slug&fields[2]=body&fields[3]=metaDescription&fields[4]=beforeContents&fields[5]=hideTitleAndDescription&fields[6]=hideContents&fields[7]=hideGuidePagesNav&fields[8]=showLastReviewedDateOnPage&fields[9]=lastReviewedDate" +
                          "&populate[applicablePhases][fields][0]=title&populate[applicablePhases][fields][1]=slug" +
                          "&populate[applicableProfessions][fields][0]=title&populate[applicableProfessions][fields][1]=slug&populate[applicableProfessions][fields][2]=plural" +
                          "&populate[Section][fields][0]=title&populate[Section][fields][1]=group&populate[Section][fields][2]=body" +
                          "&populate[Section][populate][contentModules][fields][0]=title&populate[Section][populate][contentModules][fields][1]=slug&populate[Section][populate][contentModules][fields][2]=summary&populate[Section][populate][contentModules][fields][3]=body&populate[Section][populate][contentModules][fields][4]=entryType&populate[Section][populate][contentModules][fields][5]=strength&populate[Section][populate][contentModules][fields][6]=priority&populate[Section][populate][contentModules][fields][7]=legalRequirement&populate[Section][populate][contentModules][fields][8]=notes" +
                          "&populate[Section][populate][contentModules][populate][phases][fields][0]=title&populate[Section][populate][contentModules][populate][phases][fields][1]=slug" +
                          "&populate[Section][populate][contentModules][populate][roles][fields][0]=title&populate[Section][populate][contentModules][populate][roles][fields][1]=slug" +
                          "&populate[Section][populate][contentModules][populate][links][fields][0]=title&populate[Section][populate][contentModules][populate][links][fields][1]=url&populate[Section][populate][contentModules][populate][links][fields][2]=newTab&populate[Section][populate][contentModules][populate][links][fields][3]=externalLink" +
                          "&populate[Section][populate][contentModules][populate][detailedGuides][fields][0]=title&populate[Section][populate][contentModules][populate][detailedGuides][fields][1]=slug" +
                          "&populate[Section][populate][contentModules][populate][collections][fields][0]=title&populate[Section][populate][contentModules][populate][collections][fields][1]=slug" +
                          "&populate[detailed_guide][fields][0]=title&populate[detailed_guide][fields][1]=slug&populate[detailed_guide][fields][2]=metaDescription&populate[detailed_guide][fields][3]=showLastReviewedDateOnPage&populate[detailed_guide][fields][4]=lastReviewedDate&populate[detailed_guide][fields][5]=hideContentsOnPrimaryPage&populate[detailed_guide][fields][6]=showOwnerOnPage&populate[detailed_guide][fields][7]=showApplicablePhasesOnPage&populate[detailed_guide][fields][8]=showApplicableProfessionsOnPage&populate[detailed_guide][fields][9]=customJS&populate[detailed_guide][fields][10]=customCSS&populate[detailed_guide][fields][11]=overrideOverviewTitle&populate[detailed_guide][fields][12]=showGuidePagesOnRight" +
                          "&populate[detailed_guide][populate][detailed_guide_pages][fields][0]=title&populate[detailed_guide][populate][detailed_guide_pages][fields][1]=slug&populate[detailed_guide][populate][detailed_guide_pages][fields][2]=metaDescription" +
                          "&populate[detailed_guide][populate][detailed_guide_pages][populate][applicablePhases][fields][0]=title&populate[detailed_guide][populate][detailed_guide_pages][populate][applicablePhases][fields][1]=slug" +
                          "&populate[detailed_guide][populate][detailed_guide_pages][populate][applicableProfessions][fields][0]=title&populate[detailed_guide][populate][detailed_guide_pages][populate][applicableProfessions][fields][1]=slug&populate[detailed_guide][populate][detailed_guide_pages][populate][applicableProfessions][fields][2]=plural" +
                          "&populate[detailed_guide][populate][collection][fields][0]=title&populate[detailed_guide][populate][collection][fields][1]=slug" +
                          "&populate[detailed_guide][populate][contentOwner][fields][0]=title&populate[detailed_guide][populate][contentOwner][populate][informationPage][fields][0]=urlToRedirectTo" +
                          "&populate[detailed_guide][populate][applicablePhases][fields][0]=title&populate[detailed_guide][populate][applicablePhases][fields][1]=slug" +
                          "&populate[detailed_guide][populate][applicableProfessions][fields][0]=title&populate[detailed_guide][populate][applicableProfessions][fields][1]=slug&populate[detailed_guide][populate][applicableProfessions][fields][2]=plural" +
                          "&populate[relatedContent][fields][0]=Header&populate[relatedContent][fields][1]=Content" +
                          "&populate[relatedFiles]=true";

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("CMS API returned {StatusCode} for guide page slug '{PageSlug}'", response.StatusCode, pageSlug);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<StrapiCollectionResponse<StrapiDetailedGuidePage>>(json, JsonOptions);

                var item = result?.Data?.FirstOrDefault();
                if (item is null)
                    return null;

                var guideCollections = item.Detailed_Guide?.Collection != null && !string.IsNullOrEmpty(item.Detailed_Guide.Collection.Slug) && !string.IsNullOrEmpty(item.Detailed_Guide.Collection.Title)
                    ? new List<CollectionRef> { new CollectionRef { Slug = item.Detailed_Guide.Collection.Slug!, Title = item.Detailed_Guide.Collection.Title! } }
                    : new List<CollectionRef>();
                var firstGuideCollection = guideCollections.FirstOrDefault();
                return new DetailedGuidePage
                {
                    Title = item.Title ?? string.Empty,
                    Slug = item.Slug ?? string.Empty,
                    MetaDescription = item.MetaDescription,
                    BeforeContents = item.BeforeContents,
                    Body = item.Body ?? string.Empty,
                    HideTitleAndDescription = item.HideTitleAndDescription ?? false,
                    HideContents = item.HideContents ?? false,
                    HideGuidePagesNav = item.HideGuidePagesNav ?? false,
                    GuideTitle = item.Detailed_Guide?.Title,
                    GuideSlug = item.Detailed_Guide?.Slug,
                    GuideMetaDescription = item.Detailed_Guide?.MetaDescription,
                    OverrideOverviewTitle = item.Detailed_Guide?.OverrideOverviewTitle,
                    HideContentsOnPrimaryPage = item.Detailed_Guide?.HideContentsOnPrimaryPage ?? false,
                    ShowGuidePagesOnRight = item.Detailed_Guide?.ShowGuidePagesOnRight ?? false,
                    CollectionTitle = firstGuideCollection?.Title,
                    CollectionSlug = firstGuideCollection?.Slug,
                    Collections = guideCollections,
                    SiblingPages = item.Detailed_Guide?.Detailed_Guide_Pages?
                        .Select(p => new DetailedGuidePageSummary
                        {
                            Title = p.Title ?? string.Empty,
                            Slug = p.Slug ?? string.Empty,
                            MetaDescription = p.MetaDescription,
                            Phases = p.ApplicablePhases?
                                .Where(ph => !string.IsNullOrWhiteSpace(ph.Slug) || !string.IsNullOrWhiteSpace(ph.Title))
                                .Select(ph => new TagRef { Slug = ph.Slug ?? "", Title = ph.Title ?? "" })
                                .ToList() ?? [],
                            Professions = p.ApplicableProfessions?
                                .Where(pr => !string.IsNullOrWhiteSpace(pr.Slug) || !string.IsNullOrWhiteSpace(pr.Plural) || !string.IsNullOrWhiteSpace(pr.Title))
                                .Select(pr => new TagRef { Slug = pr.Slug ?? "", Title = (pr.Plural ?? pr.Title ?? "").Trim() })
                                .ToList() ?? [],
                        })
                        .ToList() ?? [],
                    RelatedContent = item.RelatedContent?
                        .Select(r => new RelatedContentItem { Header = r.Header ?? string.Empty, Content = r.Content })
                        .ToList() ?? [],
                    Sections = item.Section?
                        .Select(section => new DetailedGuidePageSection
                        {
                            Title = section.Title ?? string.Empty,
                            Group = section.Group,
                            Body = section.Body,
                            ContentModules = section.ContentModules?
                                .Select(MapContentEntry)
                                .ToList() ?? []
                        })
                        .ToList() ?? [],
                    Phases = item.ApplicablePhases?
                        .Where(p => !string.IsNullOrWhiteSpace(p.Slug) || !string.IsNullOrWhiteSpace(p.Title))
                        .Select(p => new TagRef { Slug = p.Slug ?? "", Title = p.Title ?? "" })
                        .ToList() ?? [],
                    Professions = item.ApplicableProfessions?
                        .Select(p => (p.Plural ?? p.Title ?? "").Trim())
                        .Where(p => !string.IsNullOrWhiteSpace(p))
                        .ToList() ?? [],
                    ShowLastReviewedDateOnPage = item.ShowLastReviewedDateOnPage ?? false,
                    LastReviewedDateDisplay = FormatDateTime(item.LastReviewedDate),
                    ShowOwnerOnPage = item.Detailed_Guide?.ShowOwnerOnPage ?? true,
                    Owner = item.Detailed_Guide?.ContentOwner?.Title?.Trim(),
                    OwnerUrl = item.Detailed_Guide?.ContentOwner?.RedirectUrl?.Trim(),
                    ShowApplicablePhasesOnPage = item.Detailed_Guide?.ShowApplicablePhasesOnPage ?? false,
                    PhaseTags = item.Detailed_Guide?.ApplicablePhases?
                        .Where(p => !string.IsNullOrWhiteSpace(p.Slug) || !string.IsNullOrWhiteSpace(p.Title))
                        .Select(p => new TagRef { Slug = p.Slug ?? "", Title = p.Title ?? "" })
                        .ToList() ?? [],
                    ShowApplicableProfessionsOnPage = item.Detailed_Guide?.ShowApplicableProfessionsOnPage ?? false,
                    AudienceTags = item.Detailed_Guide?.ApplicableProfessions?
                        .Where(p => !string.IsNullOrWhiteSpace(p.Slug) || !string.IsNullOrWhiteSpace(p.Plural) || !string.IsNullOrWhiteSpace(p.Title))
                        .Select(p => new TagRef { Slug = p.Slug ?? "", Title = (p.Plural ?? p.Title ?? "").Trim() })
                        .ToList() ?? [],
                    RelatedFiles = MapRelatedFiles(item.RelatedFiles, GetCmsBaseUrl()),
                    CustomCSS = item.Detailed_Guide?.CustomCss,
                    CustomJS = item.Detailed_Guide?.CustomJs,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching detailed guide page for slug '{PageSlug}'", pageSlug);
                return null;
            }
        }

        public async Task<HtmlPage?> GetHtmlPageBySlugAsync(string slug)
        {
            try
            {
                var url = $"api/html-pages?filters[slug][$eq]={Uri.EscapeDataString(slug)}" +
                          "&fields[0]=title&fields[1]=metaDescription&fields[2]=slug&fields[3]=html&fields[4]=css&fields[5]=js";

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("CMS API returned {StatusCode} for html-page slug '{Slug}'", response.StatusCode, slug);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<StrapiCollectionResponse<StrapiHtmlPage>>(json, JsonOptions);

                var item = result?.Data?.FirstOrDefault();
                if (item is null)
                    return null;

                return new HtmlPage
                {
                    Title = item.Title ?? string.Empty,
                    MetaDescription = item.MetaDescription ?? string.Empty,
                    Slug = item.Slug ?? string.Empty,
                    Html = item.Html ?? string.Empty,
                    Css = item.Css,
                    Js = item.Js
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching html-page for slug '{Slug}'", slug);
                return null;
            }
        }

        public async Task<Roadmap?> GetRoadmapAsync()
        {
            try
            {
                const string url = "api/roadmap?fields[0]=title&fields[1]=metaDescription&fields[2]=body&fields[3]=updateHistory";

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("CMS API returned {StatusCode} for roadmap single type", response.StatusCode);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<StrapiSingleTypeResponse<StrapiRoadmap>>(json, JsonOptions);
                var item = result?.Data;

                if (item is null)
                    return null;

                return new Roadmap
                {
                    Title = item.Title ?? string.Empty,
                    MetaDescription = item.MetaDescription,
                    Body = item.Body,
                    UpdateHistory = item.UpdateHistory
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching roadmap single type");
                return null;
            }
        }

        public async Task<ToolsPage?> GetToolsPageAsync()
        {
            try
            {
                const string url = "api/tool?fields[0]=title&fields[1]=slug&fields[2]=description&fields[3]=body&populate[Tool][fields][0]=title&populate[Tool][fields][1]=url&populate[Tool][fields][2]=description&populate[Tool][fields][3]=internalOnly&populate[Tool][fields][4]=openInNewTab";

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("CMS API returned {StatusCode} for tool single type", response.StatusCode);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<StrapiSingleTypeResponse<StrapiToolsPage>>(json, JsonOptions);
                var item = result?.Data;

                if (item is null)
                    return null;

                return new ToolsPage
                {
                    Title = item.Title ?? string.Empty,
                    Slug = item.Slug,
                    Description = item.Description,
                    Body = item.Body,
                    Tools = (item.Tool ?? [])
                        .Where(tool => !string.IsNullOrWhiteSpace(tool.Title) && !string.IsNullOrWhiteSpace(tool.Url))
                        .Select(tool => new ToolsPageItem
                        {
                            Title = tool.Title ?? string.Empty,
                            Url = tool.Url,
                            Description = tool.Description,
                            InternalOnly = tool.InternalOnly ?? false,
                            OpenInNewTab = tool.OpenInNewTab ?? false
                        })
                        .ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching tool single type");
                return null;
            }
        }

        public async Task<HowManyPeoplePage?> GetHowManyPeoplePageAsync()
        {
            try
            {
                const string url = "api/how-many-people?fields[0]=title&fields[1]=slug&fields[2]=quickPickNumbers&fields[3]=description&fields[4]=dataDisclaimer&fields[5]=data";

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("CMS API returned {StatusCode} for how-many-people single type", response.StatusCode);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<StrapiSingleTypeResponse<StrapiHowManyPeople>>(json, JsonOptions);
                var item = result?.Data;

                if (item is null)
                    return null;

                return new HowManyPeoplePage
                {
                    Title = item.Title ?? string.Empty,
                    Slug = item.Slug,
                    Description = item.Description,
                    QuickPickNumbers = item.QuickPickNumbers,
                    DataDisclaimer = item.DataDisclaimer,
                    Data = item.Data
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching how-many-people single type");
                return null;
            }
        }

        public async Task<Homepage?> GetHomepageAsync()
        {
            try
            {
                const string url = "api/homepage?fields[0]=title&fields[1]=headline&fields[2]=html&fields[3]=customJS&fields[4]=customCSS";

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("CMS API returned {StatusCode} for homepage single type", response.StatusCode);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<StrapiSingleTypeResponse<StrapiHomepage>>(json, JsonOptions);
                var item = result?.Data;

                if (item is null)
                    return null;

                return new Homepage
                {
                    Title = item.Title ?? string.Empty,
                    Headline = item.Headline,
                    Html = item.Html,
                    CustomJS = item.CustomJS,
                    CustomCSS = item.CustomCSS
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching homepage single type");
                return null;
            }
        }



        public async Task<List<DocumentationSection>> GetDocumentationSectionsAsync()
        {
            try
            {
                var url = "api/documentation-sections" +
                          "?filters[enabled][$eq]=true" +
                          "&fields[0]=title&fields[1]=slug&fields[2]=order" +
                          "&populate[documentations][fields][0]=title&populate[documentations][fields][1]=slug" +
                          "&sort=order:asc" +
                          "&pagination[pageSize]=50";

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("CMS API returned {StatusCode} for documentation-sections", response.StatusCode);
                    return [];
                }

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<StrapiCollectionResponse<StrapiDocumentationSection>>(json, JsonOptions);
                if (result?.Data is null)
                    return [];

                return result.Data
                    .Where(s => !string.IsNullOrEmpty(s.Slug))
                    .Select(s => new DocumentationSection
                    {
                        Id = s.Id,
                        Title = s.Title ?? string.Empty,
                        Slug = s.Slug ?? string.Empty,
                        Order = s.Order,
                        Enabled = s.Enabled,
                        Documentations = (s.Documentations ?? [])
                            .Where(d => !string.IsNullOrEmpty(d.Slug))
                            .OrderBy(d => d.Id)
                            .Select(d => new DocumentationPageSummary { Title = d.Title ?? string.Empty, Slug = d.Slug ?? string.Empty })
                            .ToList()
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching documentation sections");
                return [];
            }
        }

        public async Task<DocumentationSection?> GetDocumentationSectionBySlugAsync(string sectionSlug)
        {
            try
            {
                var url = "api/documentation-sections" +
                          $"?filters[slug][$eq]={Uri.EscapeDataString(sectionSlug)}" +
                          "&filters[enabled][$eq]=true" +
                          "&fields[0]=title&fields[1]=slug&fields[2]=order" +
                          "&populate[documentations][fields][0]=title&populate[documentations][fields][1]=slug&populate[documentations][fields][2]=id" +
                          "&pagination[pageSize]=1";

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("CMS API returned {StatusCode} for documentation-section slug '{Slug}'", response.StatusCode, sectionSlug);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<StrapiCollectionResponse<StrapiDocumentationSection>>(json, JsonOptions);
                var item = result?.Data?.FirstOrDefault();
                if (item is null || string.IsNullOrEmpty(item.Slug))
                    return null;

                return new DocumentationSection
                {
                    Id = item.Id,
                    Title = item.Title ?? string.Empty,
                    Slug = item.Slug ?? string.Empty,
                    Order = item.Order,
                    Enabled = item.Enabled,
                    Documentations = (item.Documentations ?? [])
                        .Where(d => !string.IsNullOrEmpty(d.Slug))
                        .OrderBy(d => d.Id)
                        .Select(d => new DocumentationPageSummary { Title = d.Title ?? string.Empty, Slug = d.Slug ?? string.Empty })
                        .ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching documentation section '{Slug}'", sectionSlug);
                return null;
            }
        }

        public async Task<DocumentationPage?> GetDocumentationBySlugAsync(string sectionSlug, string pageSlug)
        {
            try
            {
                var url = "api/documentations" +
                          $"?filters[slug][$eq]={Uri.EscapeDataString(pageSlug)}" +
                          "&fields[0]=title&fields[1]=slug&fields[2]=metaDescription&fields[3]=body&fields[4]=lastReviewedDate" +
                          "&populate[documentationSection][fields][0]=slug&populate[documentationSection][fields][1]=title" +
                          "&pagination[pageSize]=1";

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("CMS API returned {StatusCode} for documentation slug '{SectionSlug}/{PageSlug}'", response.StatusCode, sectionSlug, pageSlug);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<StrapiCollectionResponse<StrapiDocumentation>>(json, JsonOptions);
                var item = result?.Data?.FirstOrDefault();
                if (item is null || string.IsNullOrEmpty(item.Slug))
                    return null;

                var sectionSlugActual = item.DocumentationSection?.Slug ?? string.Empty;
                if (!string.Equals(sectionSlugActual, sectionSlug, StringComparison.OrdinalIgnoreCase))
                    return null;

                return new DocumentationPage
                {
                    Id = item.Id,
                    Title = item.Title ?? string.Empty,
                    Slug = item.Slug ?? string.Empty,
                    MetaDescription = item.MetaDescription,
                    Body = item.Body,
                    LastReviewedDateDisplay = FormatDateTime(item.LastReviewedDate),
                    SectionSlug = sectionSlugActual,
                    SectionTitle = item.DocumentationSection?.Title ?? string.Empty
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching documentation '{SectionSlug}/{PageSlug}'", sectionSlug, pageSlug);
                return null;
            }
        }

        public async Task<PageNotification?> GetActivePageNotificationAsync(string relationFilter, string slug)
        {
            try
            {
                var now = DateTime.UtcNow.ToString("o");
                var url = $"api/page-notifications?filters[{relationFilter}][slug][$eq]={Uri.EscapeDataString(slug)}" +
                          $"&filters[validFrom][$lte]={Uri.EscapeDataString(now)}" +
                          $"&filters[validTo][$gte]={Uri.EscapeDataString(now)}" +
                          "&filters[enabled][$eq]=true" +
                          "&publicationState=live" +
                          "&fields[0]=title&fields[1]=message";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogDebug("CMS API returned {StatusCode} for page-notifications (relation={Relation}, slug={Slug})", response.StatusCode, relationFilter, slug);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<StrapiCollectionResponse<StrapiPageNotification>>(json, JsonOptions);
                var item = result?.Data?.FirstOrDefault();
                if (item is null)
                    return null;

                var message = ExtractNotificationMessage(item.Message);
                if (string.IsNullOrWhiteSpace(message))
                    return null;

                return new PageNotification
                {
                    Title = item.Title ?? "Important",
                    Message = message
                };
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error fetching page notification for {Relation} slug '{Slug}'", relationFilter, slug);
                return null;
            }
        }

        private static string ExtractNotificationMessage(JsonElement? messageElement)
        {
            if (!messageElement.HasValue)
                return string.Empty;
            var el = messageElement.Value;
            if (el.ValueKind == JsonValueKind.Null || el.ValueKind == JsonValueKind.Undefined)
                return string.Empty;
            if (el.ValueKind == JsonValueKind.String)
                return el.GetString() ?? string.Empty;
            if (el.ValueKind == JsonValueKind.Object && el.TryGetProperty("blocks", out var blocks) && blocks.ValueKind == JsonValueKind.Array)
            {
                var parts = new List<string>();
                foreach (var block in blocks.EnumerateArray())
                {
                    if (block.TryGetProperty("children", out var children))
                        foreach (var child in children.EnumerateArray())
                            if (child.TryGetProperty("text", out var text))
                                parts.Add(text.GetString() ?? string.Empty);
                }
                return string.Join(" ", parts);
            }
            if (el.ValueKind == JsonValueKind.Object && el.TryGetProperty("children", out var topChildren))
                return ExtractTextFromStrapiBlocks(topChildren);
            return string.Empty;
        }

        private static string ExtractTextFromStrapiBlocks(JsonElement children)
        {
            if (children.ValueKind != JsonValueKind.Array)
                return string.Empty;
            var parts = new List<string>();
            foreach (var node in children.EnumerateArray())
            {
                if (node.TryGetProperty("text", out var text))
                    parts.Add(text.GetString() ?? string.Empty);
                else if (node.TryGetProperty("children", out var nested))
                    parts.Add(ExtractTextFromStrapiBlocks(nested));
            }
            return string.Join(" ", parts);
        }

        public async Task<Lifecycle?> GetLifecycleAsync()
        {
            return await GetLifecycleBySlugAsync("service-delivery-lifecycle");
        }

        public async Task<Lifecycle?> GetLifecycleBySlugAsync(string slug)
        {
            try
            {
                // Strapi v5: named keys for nested populate. Do NOT use *=* on relations that have inverse relations
                // (e.g. roles.tasks, leadRole.tasks, track.tasks, outputs.tasks) or we get "Invalid key tasks".
                var url = $"api/lifecycles?filters[slug][$eq]={Uri.EscapeDataString(slug)}" +
                          "&publicationState=live" +
                          "&populate[stages][sort][0]=order:asc" +
                          "&populate[stages][populate][0]=activeTracks" +
                          "&populate[stages][populate][1]=placements" +
                          "&populate[stages][populate][placements][populate][task]=*" +
                          "&populate[stages][populate][placements][populate][overrideOutputs]=true" +
                          "&populate[stages][populate][placements][populate][overrideGuidanceLinks]=*" +
                          "&populate[stages][populate][placements][populate][task][populate][track]=true" +
                          "&populate[stages][populate][placements][populate][task][populate][roles]=true" +
                          "&populate[stages][populate][placements][populate][task][populate][leadRole]=true" +
                          "&populate[stages][populate][placements][populate][task][populate][outputs]=true" +
                          "&populate[stages][populate][placements][populate][task][populate][guidanceLinks]=*" +
                          "&populate[stages][populate][placements][populate][task][populate][howSteps]=*" +
                          "&populate[0]=lucidResources";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("CMS API returned {StatusCode} for lifecycle slug '{Slug}'. Check CmsApi:BaseUrl (e.g. http://localhost:1337/) and that the CMS is running.", response.StatusCode, slug);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                StrapiLifecycleCol? item = null;
                using (var doc = JsonDocument.Parse(json))
                {
                    if (doc.RootElement.TryGetProperty("data", out var dataEl))
                    {
                        if (dataEl.ValueKind == JsonValueKind.Array && dataEl.GetArrayLength() > 0)
                            item = JsonSerializer.Deserialize<StrapiLifecycleCol>(dataEl[0].GetRawText(), JsonOptions);
                        else if (dataEl.ValueKind == JsonValueKind.Object)
                            item = JsonSerializer.Deserialize<StrapiLifecycleCol>(dataEl.GetRawText(), JsonOptions);
                    }
                }
                if (item is null)
                {
                    _logger.LogWarning("Lifecycle slug '{Slug}': CMS returned 200 but no lifecycle in response. Check the lifecycle slug in the CMS is exactly '{Slug}' and that it is published.", slug, slug);
                    return null;
                }

                _logger.LogInformation("Lifecycle loaded from CMS: {Title} (slug: {Slug}), {StageCount} stages", item.Title, item.Slug, item.Stages?.Count ?? 0);

                return new Lifecycle
                {
                    Title = item.Title ?? string.Empty,
                    Slug = item.Slug ?? string.Empty,
                    Summary = item.Summary,
                    OwnerLabel = item.OwnerLabel,
                    AudienceLabel = item.AudienceLabel,
                    LastUpdatedLabel = item.LastUpdatedLabel,
                    DefaultView = item.DefaultView ?? "card",
                    LucidResources = (item.LucidResources ?? []).Select(x => new LinkCard
                    {
                        Label = x.Label ?? string.Empty,
                        Description = x.Description,
                        Url = x.Url ?? string.Empty,
                        Icon = x.Icon,
                        BgColourHex = x.BgColourHex
                    }).ToList(),
                    Stages = (item.Stages ?? [])
                        .OrderBy(s => s.Order)
                        .Select(s => new LifecycleStage
                        {
                            Title = s.Title ?? string.Empty,
                            Slug = s.Slug ?? string.Empty,
                            Summary = s.Summary,
                            DurationLabel = s.DurationLabel,
                            Order = s.Order,
                            ColourHex = s.ColourHex,
                            TagLabel = s.TagLabel,
                            IsCollapsedByDefault = s.IsCollapsedByDefault ?? false,
                            ActiveTracks = (s.ActiveTracks ?? []).Select(t => new TrackRef { Title = t.Title ?? string.Empty, Slug = t.Slug ?? string.Empty, ColourHex = t.ColourHex }).ToList(),
                            Placements = (s.Placements ?? [])
                                .Where(p => string.Equals(p.Visibility, "published", StringComparison.OrdinalIgnoreCase))
                                .OrderBy(p => p.Order)
                                .Select(p => new StageTaskPlacement
                                {
                                    WhenLabel = p.WhenLabel,
                                    Order = p.Order,
                                    StageNotes = p.StageNotes,
                                    OverrideGuidanceLinks = (p.OverrideGuidanceLinks ?? []).Select(l => new LinkItem { Label = l.Label ?? string.Empty, Url = l.Url ?? string.Empty, SourceLabel = l.SourceLabel, OpensInNewTab = l.OpensInNewTab ?? false }).ToList(),
                                    Visibility = p.Visibility ?? "published",
                                    Task = MapTaskRef(p.Task),
                                    OverrideOutputs = (p.OverrideOutputs ?? []).Select(o => new OutputRef { Title = o.Title ?? string.Empty, TemplateLink = o.TemplateLink }).ToList()
                                }).ToList()
                        }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching lifecycle by slug '{Slug}'", slug);
                return null;
            }
        }

        private static TaskRef MapTaskRef(StrapiTaskRef? t)
        {
            if (t is null) return new TaskRef();
            return new TaskRef
            {
                Title = t.Title ?? string.Empty,
                Slug = t.Slug ?? string.Empty,
                What = t.What,
                WhyItMatters = t.WhyItMatters,
                HowSteps = (t.HowSteps ?? []).Select(h => new HowStep { StepText = h.StepText ?? string.Empty }).ToList(),
                GuidanceLinks = (t.GuidanceLinks ?? []).Select(l => new LinkItem { Label = l.Label ?? string.Empty, Url = l.Url ?? string.Empty, SourceLabel = l.SourceLabel, OpensInNewTab = l.OpensInNewTab ?? false }).ToList(),
                Notes = t.Notes,
                Track = t.Track != null ? new TrackRef { Title = t.Track.Title ?? string.Empty, Slug = t.Track.Slug ?? string.Empty, ColourHex = t.Track.ColourHex } : null,
                Roles = (t.Roles ?? []).Select(r => new RoleRef { Title = r.Title ?? string.Empty, Slug = r.Slug ?? string.Empty, ShortCode = r.ShortCode, ColourHex = r.ColourHex }).ToList(),
                LeadRole = t.LeadRole != null ? new RoleRef { Title = t.LeadRole.Title ?? string.Empty, Slug = t.LeadRole.Slug ?? string.Empty, ShortCode = t.LeadRole.ShortCode, ColourHex = t.LeadRole.ColourHex } : null,
                Outputs = (t.Outputs ?? []).Select(o => new OutputRef { Title = o.Title ?? string.Empty, TemplateLink = o.TemplateLink }).ToList()
            };
        }

        public async Task<Phase?> GetPhaseBySlugAsync(string slug)
        {
            try
            {
                var url = $"api/phases?filters[slug][$eq]={Uri.EscapeDataString(slug)}&filters[is_active][$eq]=true" +
                          "&populate[lifecycle][fields][0]=title&populate[lifecycle][fields][1]=slug" +
                          "&populate[trackTags][fields][0]=title&populate[trackTags][fields][1]=slug" +
                          "&populate[activities][populate][0]=related_content&populate[activities][populate][1]=professionTags&populate[activities][populate][2]=trackTag&populate[activities][populate][3]=resources" +
                          "&populate[relatedGuidance][populate][0]=external_link" +
                          "&populate[relatedStandards][populate][0]=external_link" +
                          "&populate[relatedTools][populate][0]=external_link" +
                          "&populate[relatedTraining][populate][0]=external_link" +
                          "&populate[mandatory_requirements][populate][related_standard][fields][0]=title&populate[mandatory_requirements][populate][related_standard][fields][1]=slug" +
                          "&populate[outputs][populate][template_link][fields][0]=title&populate[outputs][populate][template_link][fields][1]=slug" +
                          "&populate[assurance][populate][preparation_guidance][fields][0]=title&populate[assurance][populate][preparation_guidance][fields][1]=slug";

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("CMS API returned {StatusCode} for phase slug '{Slug}'", response.StatusCode, slug);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<StrapiCollectionResponse<StrapiPhase>>(json, JsonOptions);

                var item = result?.Data?.FirstOrDefault();
                if (item is null)
                    return null;

                return new Phase
                {
                    Title = item.Title ?? string.Empty,
                    Slug = item.Slug ?? string.Empty,
                    Sequence = item.Sequence,
                    Hint = item.Hint,
                    Summary = item.Summary,
                    Purpose = item.Purpose,
                    Intent = item.Intent,
                    Duration = item.Duration,
                    TeamSize = item.TeamSize,
                    IsActive = item.IsActive ?? true,
                    LifecycleTitle = item.Lifecycle?.Title,
                    LifecycleSlug = item.Lifecycle?.Slug,
                    TrackTitles = (item.TrackTags ?? []).Select(t => t.Title ?? string.Empty).Where(s => !string.IsNullOrEmpty(s)).ToList(),
                    RelatedGuidance = MapPhaseRelatedLinks(item.Related_Guidance),
                    RelatedStandards = MapPhaseRelatedLinks(item.Related_Standards),
                    RelatedTools = MapPhaseRelatedLinks(item.Related_Tools),
                    RelatedTraining = MapPhaseRelatedLinks(item.Related_Training),
                    Activities = item.Activities?
                        .OrderBy(a => a.Order)
                        .Select(a => new PhaseActivity
                        {
                            Title = a.Title ?? string.Empty,
                            ShortDescription = a.Short_Description,
                            Description = a.Description,
                            GuidanceContent = a.Guidance_Content,
                            ProfessionTitles = (a.Profession_Tags ?? []).Select(t => t.Title ?? string.Empty).Where(s => !string.IsNullOrEmpty(s)).ToList(),
                            TrackTitle = a.Track_Tag?.Title,
                            RelatedContent = (a.Related_Content ?? [])
                                .Select(r => new PhaseRelatedLink { Title = r.Title ?? string.Empty, Url = ResolveGuidanceUrl(r) })
                                .ToList(),
                            Resources = (a.Resources ?? []).OrderBy(r => r.Order).Select(r => new ServiceManual.Models.PhaseActivityResource
                            {
                                ResourceType = r.Resource_Type ?? "link",
                                Title = r.Title ?? string.Empty,
                                Description = r.Description,
                                Url = r.Url ?? r.Media?.Url,
                                MediaUrl = r.Media?.Url
                            }).ToList()
                        })
                        .ToList() ?? [],
                    MandatoryRequirements = item.Mandatory_Requirements?
                        .Select(r => new PhaseRequirement
                        {
                            RequirementTitle = r.Requirement_Title ?? string.Empty,
                            Description = r.Description,
                            EvidenceExamples = r.Evidence_Examples,
                            RelatedStandard = r.Related_Standard != null
                                ? new PhaseRelatedLink { Title = r.Related_Standard.Title ?? string.Empty, Url = ResolveStandardUrl(r.Related_Standard) }
                                : null
                        })
                        .ToList() ?? [],
                    Outputs = item.Outputs?
                        .Select(o => new PhaseOutput
                        {
                            OutputName = o.Output_Name ?? string.Empty,
                            Description = o.Description,
                            TemplateLink = o.Template_Link != null
                                ? new PhaseRelatedLink { Title = o.Template_Link.Title ?? string.Empty, Url = ResolveGuidanceUrl(o.Template_Link) }
                                : null,
                            IsMandatory = o.Is_Mandatory ?? false
                        })
                        .ToList() ?? [],
                    Assurance = item.Assurance != null
                        ? new PhaseAssurance
                        {
                            AssuranceType = item.Assurance.Assurance_Type ?? string.Empty,
                            Description = item.Assurance.Description,
                            GovernanceBody = item.Assurance.Governance_Body,
                            PreparationGuidance = item.Assurance.Preparation_Guidance != null
                                ? new PhaseRelatedLink { Title = item.Assurance.Preparation_Guidance.Title ?? string.Empty, Url = ResolveGuidanceUrl(item.Assurance.Preparation_Guidance) }
                                : null
                        }
                        : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching phase for slug '{Slug}'", slug);
                return null;
            }
        }

        private static string ResolveGuidanceUrl(StrapiSlugRef r)
        {
            if (string.IsNullOrEmpty(r.Slug)) return "#";
            return $"/guidance/guides/{r.Slug}";
        }

        private static string ResolveStandardUrl(StrapiSlugRef r)
        {
            if (string.IsNullOrEmpty(r.Slug)) return "#";
            return $"/standards/{r.Slug}";
        }

        private static List<PhaseRelatedLink> MapPhaseRelatedLinks(List<StrapiPhaseRelatedLinkItem>? items)
        {
            if (items is null) return [];
            return items
                .Where(x => x.External_Link != null && !string.IsNullOrEmpty(x.External_Link.Url))
                .Select(x => new PhaseRelatedLink { Title = x.External_Link!.Title ?? string.Empty, Url = x.External_Link.Url! })
                .ToList();
        }

        public async Task<List<NavigationItem>> GetNavigationAsync()
        {
            try
            {
                var url = "api/navigation-items" +
                          "?filters[parent][id][$null]=true" +
                          "&publicationState=live" +
                          "&fields[0]=title&fields[1]=order&fields[2]=externalUrl" +
                          "&populate[collection][fields][0]=slug" +
                          "&populate[detailed_guide][fields][0]=slug" +
                          "&populate[children][fields][0]=title&populate[children][fields][1]=order&populate[children][fields][2]=externalUrl" +
                          "&populate[children][populate][collection][fields][0]=slug" +
                          "&populate[children][populate][detailed_guide][fields][0]=slug" +
                          "&sort=order:asc" +
                          "&pagination[pageSize]=100";

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("CMS API returned {StatusCode} for navigation. Response: {Response}", response.StatusCode, body.Length > 200 ? body[..200] + "..." : body);
                    return [];
                }

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<StrapiCollectionResponse<StrapiNavigationItem>>(json, JsonOptions);

                if (result?.Data is null)
                    return [];

                return result.Data
                    .OrderBy(i => i.Order)
                    .Select(i => MapNavigationItem(i))
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching navigation");
                return [];
            }
        }

        public async Task<List<ContentIndexItem>> GetAllPublishedContentAsync()
        {
            var items = new List<ContentIndexItem>();
            const int pageSize = 250;

            // Articles: /article/{slug}
            await AddListAsync(items, "api/articles",
                "&fields[0]=title&fields[1]=metaDescription&fields[2]=slug",
                pageSize,
                "Article",
                d => $"/article/{Uri.EscapeDataString(d.Slug?.Trim() ?? string.Empty)}",
                d => string.IsNullOrWhiteSpace(d.Slug) ? null : d.Slug.Trim());

            // Collections: /guidance/collections/{slug}
            await AddListAsync(items, "api/collections",
                "&fields[0]=title&fields[1]=metaDescription&fields[2]=slug" +
                "&populate[applicablePhases][fields][0]=slug&populate[applicablePhases][fields][1]=title" +
                "&populate[applicableProfessions][fields][0]=slug&populate[applicableProfessions][fields][1]=title",
                pageSize,
                "Collection",
                d => $"/guidance/collections/{d.Slug}",
                d => d.Slug,
                phaseTagsSelector: d => ToTagRefs(d.ApplicablePhases),
                professionTagsSelector: d => ToTagRefs(d.ApplicableProfessions));

            // Detailed guides (landing): /guidance/guides/{slug} (with collection for "Part of collection")
            await AddListAsync(items, "api/detailed-guides",
                "&fields[0]=title&fields[1]=metaDescription&fields[2]=slug&populate[collection][fields][0]=title&populate[collection][fields][1]=slug" +
                "&populate[applicableProfessions][fields][0]=slug&populate[applicableProfessions][fields][1]=title",
                pageSize,
                "Detailed Guide",
                d => $"/guidance/guides/{d.Slug}",
                d => d.Slug,
                indexSlug: d => d.Slug,
                collectionSelector: d => d.Collection != null && !string.IsNullOrEmpty(d.Collection.Slug) ? (d.Collection.Title, d.Collection.Slug) : null,
                professionTagsSelector: d => ToTagRefs(d.ApplicableProfessions));

            // Detailed guide pages: /guidance/guides/{guideSlug}/{pageSlug} (with guide's collection)
            await AddDetailedGuidePagesAsync(items, pageSize);

            // Roadmap (single type): /roadmap
            var roadmap = await GetRoadmapAsync();
            if (roadmap != null)
            {
                items.Add(new ContentIndexItem
                {
                    Title = roadmap.Title,
                    MetaDescription = roadmap.MetaDescription,
                    ContentType = "Roadmap",
                    Url = "/roadmap"
                });
            }

            if (items.Count == 0)
            {
                _logger.LogWarning("Content index fetch returned 0 items. Check CMS credentials and find permissions for collections, detailed-guides, detailed-guide-pages, roadmap and job-specifications.");
            }

            return items.OrderBy(i => i.ContentType).ThenBy(i => i.Title).ToList();
        }

        public async Task<List<ContentEntry>> GetContentEntriesAsync(string? strength = null, string? phaseSlug = null, string? roleSlug = null)
        {
            try
            {
                var url = "api/content-entries?" +
                          "fields[0]=title&fields[1]=slug&fields[2]=summary&fields[3]=body&fields[4]=entryType&fields[5]=strength&fields[6]=priority&fields[7]=legalRequirement&fields[8]=notes" +
                          "&populate[phases][fields][0]=title&populate[phases][fields][1]=slug" +
                          "&populate[roles][fields][0]=title&populate[roles][fields][1]=slug";

                if (!string.IsNullOrWhiteSpace(strength))
                    url += "&filters[strength][$eq]=" + Uri.EscapeDataString(strength.Trim());

                if (!string.IsNullOrWhiteSpace(phaseSlug))
                    url += "&filters[phases][slug][$eq]=" + Uri.EscapeDataString(phaseSlug.Trim());

                if (!string.IsNullOrWhiteSpace(roleSlug))
                    url += "&filters[roles][slug][$eq]=" + Uri.EscapeDataString(roleSlug.Trim());

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("CMS API returned {StatusCode} for content entries", response.StatusCode);
                    return [];
                }

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<StrapiCollectionResponse<StrapiContentEntryListItem>>(json, JsonOptions);

                return result?.Data?.Select(MapContentEntry).ToList() ?? [];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching content entries");
                return [];
            }
        }

        public async Task<List<ContentEntry>> GetAllContentModulesAlphabeticalAsync()
        {
            const int pageSize = 100;
            const string fields =
                "fields[0]=title&fields[1]=slug&fields[2]=summary&fields[3]=body&fields[4]=entryType&fields[5]=strength&fields[6]=priority&fields[7]=legalRequirement&fields[8]=notes" +
                "&populate[phases][fields][0]=title&populate[phases][fields][1]=slug" +
                "&populate[roles][fields][0]=title&populate[roles][fields][1]=slug";

            var all = new List<ContentEntry>();
            try
            {
                for (var page = 1; ; page++)
                {
                    var url = "api/content-entries?publicationState=live" +
                              "&pagination[pageSize]=" + pageSize +
                              "&pagination[page]=" + page +
                              "&" + fields;

                    var response = await _httpClient.GetAsync(url);
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("CMS API returned {StatusCode} for content-entries page {Page}", response.StatusCode, page);
                        break;
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<StrapiCollectionResponse<StrapiContentEntryListItem>>(json, JsonOptions);
                    var batch = result?.Data;
                    if (batch == null || batch.Count == 0)
                        break;

                    foreach (var item in batch)
                        all.Add(MapContentEntry(item));

                    if (batch.Count < pageSize)
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all content modules");
            }

            return all
                .OrderBy(e => e.Title, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public async Task<List<ServiceStandardSummary>> GetServiceStandardsAsync()
        {
            try
            {
                const string url = "api/service-standards?fields[0]=title&fields[1]=point&fields[2]=slug&fields[3]=description&sort[0]=point:asc&sort[1]=title:asc";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("CMS API returned {StatusCode} for service standards", response.StatusCode);
                    return [];
                }

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<StrapiCollectionResponse<StrapiServiceStandard>>(json, JsonOptions);

                return result?.Data?
                    .Select(item => new ServiceStandardSummary
                    {
                        Title = item.Title ?? string.Empty,
                        Point = item.Point ?? 0,
                        Slug = item.Slug ?? string.Empty,
                        Description = item.Description,
                    })
                    .OrderBy(s => s.Point == 0 ? int.MaxValue : s.Point)
                    .ThenBy(s => s.Title)
                    .ToList() ?? [];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching service standards");
                return [];
            }
        }

        public async Task<ServiceStandardPage?> GetServiceStandardBySlugAsync(string slug)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(slug))
                    return null;

                var url = "api/service-standards?filters[slug][$eq]=" + Uri.EscapeDataString(slug.Trim()) +
                          "&fields[0]=title&fields[1]=point&fields[2]=slug&fields[3]=description&fields[4]=body" +
                          "&populate[Section][fields][0]=title&populate[Section][fields][1]=description" +
                          "&populate[Section][populate][content_entries][fields][0]=title" +
                          "&populate[Section][populate][content_entries][fields][1]=slug" +
                          "&populate[Section][populate][content_entries][fields][2]=summary" +
                          "&populate[Section][populate][content_entries][fields][3]=body" +
                          "&populate[Section][populate][content_entries][fields][4]=entryType" +
                          "&populate[Section][populate][content_entries][fields][5]=strength" +
                          "&populate[Section][populate][content_entries][fields][6]=priority" +
                          "&populate[Section][populate][content_entries][fields][7]=legalRequirement" +
                          "&populate[Section][populate][content_entries][fields][8]=notes" +
                          "&populate[Section][populate][content_entries][populate][phases][fields][0]=title" +
                          "&populate[Section][populate][content_entries][populate][phases][fields][1]=slug" +
                          "&populate[Section][populate][content_entries][populate][roles][fields][0]=title" +
                          "&populate[Section][populate][content_entries][populate][roles][fields][1]=slug";

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("CMS API returned {StatusCode} for service standard slug '{Slug}'", response.StatusCode, slug);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<StrapiCollectionResponse<StrapiServiceStandard>>(json, JsonOptions);
                var item = result?.Data?.FirstOrDefault();
                if (item is null)
                    return null;

                return new ServiceStandardPage
                {
                    Title = item.Title ?? string.Empty,
                    Point = item.Point ?? 0,
                    Slug = item.Slug ?? string.Empty,
                    Description = item.Description,
                    Body = item.Body,
                    Sections = (item.Section ?? [])
                        .Select(s => new ServiceStandardSection
                        {
                            Title = s.Title ?? string.Empty,
                            Description = s.Description,
                            ContentEntries = (s.ContentEntries ?? [])
                                .Select(MapContentEntry)
                                .ToList(),
                        })
                        .ToList(),
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching service standard for slug '{Slug}'", slug);
                return null;
            }
        }

        private static ContentEntry MapContentEntry(StrapiContentEntryListItem item)
        {
            return new ContentEntry
            {
                Title = item.Title ?? string.Empty,
                Slug = item.Slug ?? string.Empty,
                Summary = item.Summary,
                Body = item.Body,
                EntryType = item.EntryType,
                Strength = item.Strength,
                Priority = item.Priority,
                LegalRequirement = item.LegalRequirement ?? false,
                Notes = item.Notes,
                Phases = item.Phases?
                    .Where(p => !string.IsNullOrWhiteSpace(p.Slug) || !string.IsNullOrWhiteSpace(p.Title))
                    .Select(p => new TagRef { Slug = p.Slug ?? string.Empty, Title = p.Title ?? string.Empty })
                    .ToList() ?? [],
                Roles = item.Roles?
                    .Where(r => !string.IsNullOrWhiteSpace(r.Slug) || !string.IsNullOrWhiteSpace(r.Title))
                    .Select(r => new TagRef { Slug = r.Slug ?? string.Empty, Title = r.Title ?? string.Empty })
                    .ToList() ?? [],
                Links = MapContentEntryLinks(item)
            };
        }

        private static List<ContentEntryLink> MapContentEntryLinks(StrapiContentEntryListItem item)
        {
            var links = new List<ContentEntryLink>();

            if (item.Links != null)
            {
                links.AddRange(item.Links
                    .Where(link => !string.IsNullOrWhiteSpace(link.Title) && !string.IsNullOrWhiteSpace(link.Url))
                    .Select(link => new ContentEntryLink
                    {
                        Title = link.Title!.Trim(),
                        Url = link.Url!.Trim(),
                        OpenInNewTab = link.NewTab,
                        ExternalLink = link.ExternalLink ?? false,
                    }));
            }

            if (item.DetailedGuides != null)
            {
                links.AddRange(item.DetailedGuides
                    .Where(guide => !string.IsNullOrWhiteSpace(guide.Title) && !string.IsNullOrWhiteSpace(guide.Slug))
                    .Select(guide => new ContentEntryLink
                    {
                        Title = guide.Title!.Trim(),
                        Url = "/guidance/guides/" + Uri.EscapeDataString(guide.Slug!.Trim())
                    }));
            }

            if (item.Collections != null)
            {
                links.AddRange(item.Collections
                    .Where(collection => !string.IsNullOrWhiteSpace(collection.Title) && !string.IsNullOrWhiteSpace(collection.Slug))
                    .Select(collection => new ContentEntryLink
                    {
                        Title = collection.Title!.Trim(),
                        Url = "/guidance/collections/" + Uri.EscapeDataString(collection.Slug!.Trim())
                    }));
            }

            return links;
        }

        public async Task<string?> GetRedirectUrlByShortUrlAsync(string shortUrl)
        {
            if (string.IsNullOrWhiteSpace(shortUrl))
                return null;
            try
            {
                var url = "api/redirectors?filters[shortURL][$eq]=" + Uri.EscapeDataString(shortUrl.Trim()) +
                          "&publicationState=live";
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("CMS API returned {StatusCode} for redirect shortUrl '{ShortUrl}'", response.StatusCode, shortUrl);
                    return null;
                }
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<StrapiCollectionResponse<StrapiRedirector>>(json, JsonOptions);
                var item = result?.Data?.FirstOrDefault();
                return string.IsNullOrWhiteSpace(item?.UrlToRedirectTo) ? null : item.UrlToRedirectTo!.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching redirect for shortUrl '{ShortUrl}'", shortUrl);
                return null;
            }
        }

        public async Task<PathRedirect?> GetPathRedirectByOldPathAsync(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            var normalizedPath = path.Trim();
            if (!normalizedPath.StartsWith("/", StringComparison.Ordinal))
                normalizedPath = "/" + normalizedPath;

            var withSlashNoTrailing = normalizedPath.TrimEnd('/');
            var pathVariants = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                normalizedPath,
                withSlashNoTrailing,
                withSlashNoTrailing + "/",
                normalizedPath.TrimStart('/'),
                withSlashNoTrailing.TrimStart('/'),
                withSlashNoTrailing.TrimStart('/') + "/"
            };

            try
            {
                // Redirect-301 entries store rules as a JSON 'mapping' array — fetch all and match in-memory.
                const string url = "api/redirect-301s?fields[0]=mapping&publicationState=live&pagination[pageSize]=100";
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning(
                        "CMS API returned {StatusCode} for path redirects. If 403, enable Public find permission for '301 Redirects' in Strapi. Response: {Response}",
                        response.StatusCode, body.Length > 200 ? body[..200] + "..." : body);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<StrapiCollectionResponse<StrapiRedirect301>>(json, JsonOptions);
                if (result?.Data == null) return null;

                foreach (var entry in result.Data)
                {
                    if (entry.Mapping == null) continue;
                    foreach (var rule in entry.Mapping)
                    {
                        if (string.IsNullOrWhiteSpace(rule.OldPath) || string.IsNullOrWhiteSpace(rule.NewPath))
                            continue;
                        if (pathVariants.Contains(rule.OldPath))
                            return new PathRedirect
                            {
                                OldPath = rule.OldPath,
                                NewPath = rule.NewPath.Trim(),
                                UseInterimPage = rule.UseInterimPage
                            };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching path redirects for '{Path}'", normalizedPath);
            }

            return null;
        }

        private async Task AddListAsync(List<ContentIndexItem> items, string endpoint, string fields, int pageSize,
            string contentType, Func<StrapiContentListItem, string> urlSelector, Func<StrapiContentListItem, string?> slugSelector,
            Func<StrapiContentListItem, string?>? indexSlug = null,
            Func<StrapiContentListItem, (string? Title, string? Slug)?>? collectionSelector = null,
            Func<StrapiContentListItem, List<TagRef>>? phaseTagsSelector = null,
            Func<StrapiContentListItem, List<TagRef>>? professionTagsSelector = null)
        {
            try
            {
                var url = $"{endpoint}?pagination[pageSize]={pageSize}{fields}";
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Content index endpoint {Endpoint} returned {StatusCode}. Response: {Response}", endpoint, response.StatusCode, body.Length > 200 ? body[..200] + "..." : body);
                    return;
                }
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<StrapiCollectionResponse<StrapiContentListItem>>(json, JsonOptions);
                if (result?.Data == null) return;
                foreach (var d in result.Data.Where(d => !string.IsNullOrEmpty(slugSelector(d))))
                {
                    var collection = collectionSelector?.Invoke(d);
                    items.Add(new ContentIndexItem
                    {
                        Title = d.Title ?? string.Empty,
                        MetaDescription = d.MetaDescription,
                        ContentType = contentType,
                        Url = urlSelector(d),
                        Slug = indexSlug?.Invoke(d),
                        CollectionTitle = collection?.Title,
                        CollectionSlug = collection?.Slug,
                        ApplicablePhaseTags = phaseTagsSelector?.Invoke(d) ?? [],
                        ApplicableProfessionTags = professionTagsSelector?.Invoke(d) ?? []
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error listing {Endpoint} for content index", endpoint);
            }
        }

        private async Task AddDetailedGuidePagesAsync(List<ContentIndexItem> items, int pageSize)
        {
            try
            {
                var url = "api/detailed-guide-pages?pagination[pageSize]=" + pageSize +
                         "&fields[0]=title&fields[1]=metaDescription&fields[2]=slug" +
                         "&populate[detailed_guide][fields][0]=slug" +
                         "&populate[detailed_guide][populate][collection][fields][0]=title&populate[detailed_guide][populate][collection][fields][1]=slug" +
                         "&populate[applicablePhases][fields][0]=slug&populate[applicablePhases][fields][1]=title" +
                         "&populate[applicableProfessions][fields][0]=slug&populate[applicableProfessions][fields][1]=title";
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Content index endpoint api/detailed-guide-pages returned {StatusCode}. Response: {Response}", response.StatusCode, body.Length > 200 ? body[..200] + "..." : body);
                    return;
                }
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<StrapiCollectionResponse<StrapiDetailedGuidePageListItem>>(json, JsonOptions);
                if (result?.Data == null) return;
                foreach (var d in result.Data)
                {
                    var guideSlug = d.Detailed_Guide?.Slug;
                    if (string.IsNullOrEmpty(guideSlug) || string.IsNullOrEmpty(d.Slug)) continue;
                    var coll = d.Detailed_Guide?.Collection;
                    items.Add(new ContentIndexItem
                    {
                        Title = d.Title ?? string.Empty,
                        MetaDescription = d.MetaDescription,
                        ContentType = "Detailed Guide Page",
                        Url = $"/guidance/guides/{guideSlug}/{d.Slug}",
                        ParentContentType = "Detailed Guide",
                        ParentSlug = guideSlug,
                        CollectionTitle = coll != null && !string.IsNullOrEmpty(coll.Slug) ? coll.Title : null,
                        CollectionSlug = coll?.Slug,
                        ApplicablePhaseTags = ToTagRefs(d.ApplicablePhases),
                        ApplicableProfessionTags = ToTagRefs(d.ApplicableProfessions)
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error listing detailed-guide-pages for content index");
            }
        }

        private async Task AddPhaseListAsync(List<ContentIndexItem> items, int pageSize)
        {
            try
            {
                var lifecycle = await GetLifecycleBySlugAsync("service-delivery-lifecycle");
                if (lifecycle?.Stages == null) return;
                foreach (var stage in lifecycle.Stages.OrderBy(s => s.Order))
                {
                    if (string.IsNullOrEmpty(stage.Slug)) continue;
                    items.Add(new ContentIndexItem
                    {
                        Title = stage.Title,
                        MetaDescription = stage.Summary,
                        ContentType = "Lifecycle Stage",
                        Url = $"/lifecycle#ph-{stage.Slug}",
                        ParentContentType = "Lifecycle"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error listing lifecycle stages for content index");
            }
        }

        private static NavigationItem MapNavigationItem(StrapiNavigationItem item)
        {
            return new NavigationItem
            {
                Id = item.Id,
                Title = item.Title ?? string.Empty,
                Order = item.Order,
                Url = ResolveNavigationUrl(item),
                Children = item.Children?
                    .OrderBy(c => c.Order)
                    .Select(c => MapNavigationItem(c))
                    .ToList() ?? []
            };
        }

        private static string? ResolveNavigationUrl(StrapiNavigationItem item)
        {
            if (!string.IsNullOrEmpty(item.ExternalUrl))
                return item.ExternalUrl;

            if (item.Collection?.Slug is not null)
                return $"/guidance/collections/{item.Collection.Slug}";

            if (item.Detailed_Guide?.Slug is not null)
                return $"/guidance/guides/{item.Detailed_Guide.Slug}";

            return null;
        }

        private static string? FormatDate(string? isoDate)
        {
            if (string.IsNullOrEmpty(isoDate)) return null;
            if (DateOnly.TryParse(isoDate, out var date))
                return date.ToString("d MMMM yyyy");
            return isoDate;
        }

        /// <summary>Formats an ISO 8601 datetime (e.g. from Strapi lastReviewedDate) as "7 January 2026".</summary>
        private static string? FormatDateTime(string? isoDateTime)
        {
            if (string.IsNullOrEmpty(isoDateTime)) return null;
            if (DateTime.TryParse(isoDateTime, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
                return dt.ToString("d MMMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("en-GB"));
            return null;
        }

        private static string? ResolveArticleRouteKey(string? slug, string? documentId, int id)
        {
            if (!string.IsNullOrWhiteSpace(slug))
                return slug.Trim();
            if (!string.IsNullOrWhiteSpace(documentId))
                return documentId.Trim();
            return id > 0 ? id.ToString(System.Globalization.CultureInfo.InvariantCulture) : null;
        }

        private static string? ResolveArticleLeadImageUrl(StrapiArticle item, string? cmsBaseUrl)
        {
            var candidate = item.LeadImage?.Url ?? item.LeadImageUrl;
            if (string.IsNullOrWhiteSpace(candidate))
                return null;

            if (Uri.TryCreate(candidate, UriKind.Absolute, out _))
                return candidate;

            if (string.IsNullOrWhiteSpace(cmsBaseUrl))
                return candidate;

            return new Uri(new Uri(cmsBaseUrl.TrimEnd('/') + "/"), candidate.TrimStart('/')).ToString();
        }

        private static string? ResolveArticleLeadImageAlt(StrapiArticle item)
        {
            if (!string.IsNullOrWhiteSpace(item.LeadImageAlt))
                return item.LeadImageAlt;

            if (!string.IsNullOrWhiteSpace(item.LeadImageAlternativeText))
                return item.LeadImageAlternativeText;

            if (!string.IsNullOrWhiteSpace(item.LeadImage?.AlternativeText))
                return item.LeadImage.AlternativeText;

            return null;
        }

        private static List<TagRef> ToTagRefs(List<StrapiTagRef>? list) =>
            list?.Where(t => !string.IsNullOrEmpty(t.Slug) || !string.IsNullOrEmpty(t.Title))
                .Select(t => new TagRef { Slug = t.Slug ?? "", Title = t.Title ?? "" }).ToList() ?? [];

        private static List<TagRef> ToTagRefs(StrapiTagRef? single) =>
            single != null && (!string.IsNullOrEmpty(single.Slug) || !string.IsNullOrEmpty(single.Title))
                ? [new TagRef { Slug = single.Slug ?? "", Title = single.Title ?? "" }]
                : [];

        /// <summary>Maps API content type to display label for collection listing (GOV.UK style).</summary>
        private static string? ContentTypeLabel(string? type)
        {
            return type switch
            {
                "detailed_guide" => "Guidance",
                "detailed_guide_page" => "Guidance",
                "job_specification" => "Job description",
                "external_link" => "External link",
                _ => null
            };
        }

        // ── Strapi v5 response shapes ────────────────────────────────────────────

        private class StrapiCollectionResponse<T>
        {
            public List<T>? Data { get; set; }
        }

        private class StrapiRedirector
        {
            [JsonPropertyName("shortURL")]
            public string? ShortURL { get; set; }
            [JsonPropertyName("urlToRedirectTo")]
            public string? UrlToRedirectTo { get; set; }
        }

        private class StrapiRedirect301
        {
            [JsonPropertyName("mapping")]
            public List<StrapiRedirectMapping>? Mapping { get; set; }
        }

        private class StrapiRedirectMapping
        {
            [JsonPropertyName("oldPath")]
            public string? OldPath { get; set; }
            [JsonPropertyName("newPath")]
            public string? NewPath { get; set; }
            [JsonPropertyName("useInterimPage")]
            public bool UseInterimPage { get; set; }
        }

        private class StrapiSingleTypeResponse<T>
        {
            public T? Data { get; set; }
        }

        private class StrapiRoadmap
        {
            public string? Title { get; set; }
            public string? MetaDescription { get; set; }
            public string? Body { get; set; }
            public string? UpdateHistory { get; set; }
        }

        private class StrapiHowManyPeople
        {
            public string? Title { get; set; }
            public string? Slug { get; set; }
            public string? QuickPickNumbers { get; set; }
            public JsonElement? Data { get; set; }
            public string? Description { get; set; }
            public string? DataDisclaimer { get; set; }
        }

        private class StrapiToolsPage
        {
            public string? Title { get; set; }
            public string? Slug { get; set; }
            public string? Description { get; set; }
            public string? Body { get; set; }
            public List<StrapiToolItem>? Tool { get; set; }
        }

        private class StrapiToolItem
        {
            public string? Title { get; set; }
            public string? Url { get; set; }
            public bool? InternalOnly { get; set; }
            public string? Description { get; set; }
            public bool? OpenInNewTab { get; set; }
        }

        private class StrapiGuidanceArea
        {
            public string? Name { get; set; }
            public string? Slug { get; set; }
            public string? Summary { get; set; }
            public string? Description { get; set; }
            public string? ColourHex { get; set; }
            public List<StrapiTagRef>? FeaturedProfessions { get; set; }
            public List<StrapiGuidanceCollection>? Collections { get; set; }
        }

        private class StrapiGuidanceCollection
        {
            public string? Title { get; set; }
            public string? Slug { get; set; }
            public string? Url { get; set; }
            public string? ContentType { get; set; }
            public string? Description { get; set; }
            public int ItemCount { get; set; }
            public bool Featured { get; set; }
            public List<string>? Tags { get; set; }
            public List<StrapiTagRef>? ApplicableProfessions { get; set; }
            public List<StrapiSlugRef>? AlsoInAreas { get; set; }
        }

        private class StrapiHomepage
        {
            public string? Title { get; set; }
            public string? Headline { get; set; }
            public string? Html { get; set; }
            public string? CustomJS { get; set; }
            public string? CustomCSS { get; set; }
        }

        private class StrapiSlugRef
        {
            public string? Title { get; set; }
            public string? Slug { get; set; }
        }

        /// <summary>Deserializes tag refs from either a direct array or Strapi v5 wrapper { "data": [ ... ] } (each element may be { slug, title } or { id, attributes: { slug, title } }).</summary>
        private sealed class StrapiTagRefListConverter : JsonConverter<List<StrapiTagRef>?>
        {
            public override List<StrapiTagRef>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Null) return null;
                if (reader.TokenType == JsonTokenType.StartArray)
                    return JsonSerializer.Deserialize<List<StrapiTagRef>>(ref reader, options);
                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.PropertyName && reader.GetString() == "data")
                        {
                            reader.Read();
                            if (reader.TokenType != JsonTokenType.StartArray) return null;
                            var list = new List<StrapiTagRef>();
                            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                            {
                                var tag = ReadOneTagRef(ref reader, options);
                                if (tag != null) list.Add(tag);
                            }
                            return list;
                        }
                        if (reader.TokenType == JsonTokenType.PropertyName) { reader.Read(); reader.Skip(); }
                    }
                }
                return null;
            }

            private static StrapiTagRef? ReadOneTagRef(ref Utf8JsonReader reader, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartObject) return null;
                string? slug = null, title = null, plural = null;
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject) break;
                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        var name = reader.GetString();
                        reader.Read();
                        if (name == "attributes" && reader.TokenType == JsonTokenType.StartObject)
                        {
                            while (reader.Read())
                            {
                                if (reader.TokenType == JsonTokenType.EndObject) break;
                                if (reader.TokenType == JsonTokenType.PropertyName)
                                {
                                    var n = reader.GetString();
                                    reader.Read();
                                    if (n == "slug") slug = reader.GetString();
                                    else if (n == "title") title = reader.GetString();
                                    else if (n == "plural") plural = reader.GetString();
                                }
                            }
                        }
                        else if (name == "slug") slug = reader.GetString();
                        else if (name == "title") title = reader.GetString();
                        else if (name == "plural") plural = reader.GetString();
                        else reader.Skip();
                    }
                }
                return (slug != null || title != null || plural != null)
                    ? new StrapiTagRef { Slug = slug ?? "", Title = title ?? "", Plural = plural }
                    : null;
            }

            public override void Write(Utf8JsonWriter writer, List<StrapiTagRef>? value, JsonSerializerOptions options) =>
                throw new NotImplementedException();
        }

        private class StrapiFileItem
        {
            [JsonPropertyName("name")]
            public string? Name { get; set; }
            [JsonPropertyName("url")]
            public string? Url { get; set; }
            [JsonPropertyName("size")]
            public decimal Size { get; set; }
            [JsonPropertyName("mime")]
            public string? Mime { get; set; }
            [JsonPropertyName("ext")]
            public string? Ext { get; set; }
            [JsonPropertyName("caption")]
            public string? Caption { get; set; }
        }

        /// <summary>Deserializes relatedFiles from array or Strapi wrapper { "data": [ ... ] }; each item may be flat or have "attributes".</summary>
        private sealed class StrapiRelatedFilesConverter : JsonConverter<List<StrapiFileItem>?>
        {
            public override List<StrapiFileItem>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Null) return null;
                if (reader.TokenType == JsonTokenType.StartArray)
                    return JsonSerializer.Deserialize<List<StrapiFileItem>>(ref reader, options);
                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.PropertyName && reader.GetString() == "data")
                        {
                            reader.Read();
                            if (reader.TokenType != JsonTokenType.StartArray) return null;
                            return JsonSerializer.Deserialize<List<StrapiFileItem>>(ref reader, options);
                        }
                        if (reader.TokenType == JsonTokenType.PropertyName) { reader.Read(); reader.Skip(); }
                    }
                }
                return null;
            }
            public override void Write(Utf8JsonWriter writer, List<StrapiFileItem>? value, JsonSerializerOptions options) =>
                throw new NotImplementedException();
        }

        private static string FormatFileSize(decimal bytes)
        {
            if (bytes <= 0) return "0 B";
            string[] units = { "B", "KB", "MB", "GB" };
            int u = 0;
            decimal n = bytes;
            while (n >= 1024 && u < units.Length - 1) { n /= 1024; u++; }
            return u == 0 ? $"{n:F0} {units[u]}" : $"{n:F1} {units[u]}";
        }

        private static string FileTypeFromMimeOrExt(string? mime, string? ext)
        {
            if (!string.IsNullOrWhiteSpace(ext)) return ext.TrimStart('.').ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(mime)) return "File";
            var part = mime.Split('/').LastOrDefault();
            return string.IsNullOrEmpty(part) ? "File" : part.ToUpperInvariant();
        }

        private static string NormaliseHex(string? value)
        {
            var trimmed = value?.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                return "#1d70b8";

            return trimmed.StartsWith('#') ? trimmed : $"#{trimmed}";
        }

        private static string CoalesceSlug(string? slug, string? title)
        {
            if (!string.IsNullOrWhiteSpace(slug))
                return slug.Trim();

            return ToSlug(title);
        }

        private static string ToSlug(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var chars = value.Trim().ToLowerInvariant();
            var result = new List<char>(chars.Length);
            var previousDash = false;

            foreach (var ch in chars)
            {
                if (char.IsLetterOrDigit(ch))
                {
                    result.Add(ch);
                    previousDash = false;
                    continue;
                }

                if (!previousDash)
                {
                    result.Add('-');
                    previousDash = true;
                }
            }

            var slug = new string(result.ToArray()).Trim('-');
            return slug;
        }

        private static List<RelatedFileItem> MapRelatedFiles(List<StrapiFileItem>? files, string? cmsBaseUrl)
        {
            if (files == null || files.Count == 0) return [];
            var baseUrl = (cmsBaseUrl ?? "").TrimEnd('/');
            var result = new List<RelatedFileItem>();
            foreach (var f in files)
            {
                var url = (f.Url ?? "").Trim();
                if (string.IsNullOrEmpty(url)) continue;
                if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(baseUrl))
                    url = baseUrl + (url.StartsWith("/") ? url : "/" + url);
                result.Add(new RelatedFileItem
                {
                    Name = f.Name ?? "Download",
                    Url = url,
                    SizeDisplay = FormatFileSize(f.Size),
                    FileType = FileTypeFromMimeOrExt(f.Mime, f.Ext),
                    Caption = string.IsNullOrWhiteSpace(f.Caption) ? null : f.Caption.Trim()
                });
            }
            return result;
        }

        /// <summary>Deserializes phases from either a direct array or Strapi v5 wrapper { "data": [ ... ] }.</summary>
        private sealed class StrapiPhasesConverter : JsonConverter<List<StrapiPhaseSummary>?>
        {
            public override List<StrapiPhaseSummary>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Null)
                    return null;
                if (reader.TokenType == JsonTokenType.StartArray)
                    return JsonSerializer.Deserialize<List<StrapiPhaseSummary>>(ref reader, options);
                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.PropertyName && reader.GetString() == "data")
                        {
                            reader.Read();
                            return reader.TokenType == JsonTokenType.StartArray
                                ? JsonSerializer.Deserialize<List<StrapiPhaseSummary>>(ref reader, options)
                                : null;
                        }
                        if (reader.TokenType == JsonTokenType.PropertyName)
                        {
                            reader.Read();
                            reader.Skip();
                        }
                    }
                }
                return null;
            }

            public override void Write(Utf8JsonWriter writer, List<StrapiPhaseSummary>? value, JsonSerializerOptions options) =>
                throw new NotImplementedException();
        }

        /* New lifecycle collection model (spec) */
        private class StrapiLifecycleCol
        {
            public string? Title { get; set; }
            public string? Slug { get; set; }
            public string? Summary { get; set; }
            [JsonPropertyName("ownerLabel")]
            public string? OwnerLabel { get; set; }
            [JsonPropertyName("audienceLabel")]
            public string? AudienceLabel { get; set; }
            [JsonPropertyName("lastUpdatedLabel")]
            public string? LastUpdatedLabel { get; set; }
            [JsonPropertyName("defaultView")]
            public string? DefaultView { get; set; }
            [JsonPropertyName("lucidResources")]
            public List<StrapiLinkCard>? LucidResources { get; set; }
            public List<StrapiLifecycleStageCol>? Stages { get; set; }
        }

        private class StrapiLinkCard
        {
            public string? Label { get; set; }
            public string? Description { get; set; }
            public string? Url { get; set; }
            public string? Icon { get; set; }
            [JsonPropertyName("bgColourHex")]
            public string? BgColourHex { get; set; }
        }

        private class StrapiLifecycleStageCol
        {
            public string? Title { get; set; }
            public string? Slug { get; set; }
            public string? Summary { get; set; }
            [JsonPropertyName("durationLabel")]
            public string? DurationLabel { get; set; }
            public int Order { get; set; }
            [JsonPropertyName("colourHex")]
            public string? ColourHex { get; set; }
            [JsonPropertyName("tagLabel")]
            public string? TagLabel { get; set; }
            [JsonPropertyName("isCollapsedByDefault")]
            public bool? IsCollapsedByDefault { get; set; }
            [JsonPropertyName("activeTracks")]
            public List<StrapiTrackRef>? ActiveTracks { get; set; }
            public List<StrapiStageTaskPlacementCol>? Placements { get; set; }
        }

        private class StrapiStageTaskPlacementCol
        {
            [JsonPropertyName("whenLabel")]
            public string? WhenLabel { get; set; }
            public int Order { get; set; }
            [JsonPropertyName("stageNotes")]
            public string? StageNotes { get; set; }
            [JsonPropertyName("overrideGuidanceLinks")]
            public List<StrapiLinkItem>? OverrideGuidanceLinks { get; set; }
            public string? Visibility { get; set; }
            public StrapiTaskRef? Task { get; set; }
            [JsonPropertyName("overrideOutputs")]
            public List<StrapiOutputRef>? OverrideOutputs { get; set; }
        }

        private class StrapiTaskRef
        {
            public string? Title { get; set; }
            public string? Slug { get; set; }
            public string? What { get; set; }
            [JsonPropertyName("whyItMatters")]
            public string? WhyItMatters { get; set; }
            [JsonPropertyName("howSteps")]
            public List<StrapiHowStep>? HowSteps { get; set; }
            [JsonPropertyName("guidanceLinks")]
            public List<StrapiLinkItem>? GuidanceLinks { get; set; }
            public string? Notes { get; set; }
            public StrapiTrackRef? Track { get; set; }
            public List<StrapiRoleRef>? Roles { get; set; }
            [JsonPropertyName("leadRole")]
            public StrapiRoleRef? LeadRole { get; set; }
            public List<StrapiOutputRef>? Outputs { get; set; }
        }

        private class StrapiLinkItem
        {
            public string? Label { get; set; }
            public string? Url { get; set; }
            [JsonPropertyName("sourceLabel")]
            public string? SourceLabel { get; set; }
            [JsonPropertyName("opensInNewTab")]
            public bool? OpensInNewTab { get; set; }
        }

        private class StrapiHowStep
        {
            [JsonPropertyName("stepText")]
            public string? StepText { get; set; }
        }

        private class StrapiTrackRef
        {
            public string? Title { get; set; }
            public string? Slug { get; set; }
            [JsonPropertyName("colourHex")]
            public string? ColourHex { get; set; }
        }

        private class StrapiRoleRef
        {
            public string? Title { get; set; }
            public string? Slug { get; set; }
            [JsonPropertyName("shortCode")]
            public string? ShortCode { get; set; }
            [JsonPropertyName("colourHex")]
            public string? ColourHex { get; set; }
        }

        private class StrapiOutputRef
        {
            public string? Title { get; set; }
            [JsonPropertyName("templateLink")]
            public string? TemplateLink { get; set; }
        }

        private class StrapiLifecycle
        {
            public string? Title { get; set; }
            public string? Slug { get; set; }
            public string? Summary { get; set; }
            public string? Description { get; set; }
            public StrapiMedia? Diagram { get; set; }
            public string? Version { get; set; }
            [JsonPropertyName("is_active")]
            public bool? IsActive { get; set; }
            [JsonConverter(typeof(StrapiPhasesConverter))]
            public List<StrapiPhaseSummary>? Phases { get; set; }
            [JsonPropertyName("relatedGuidance")]
            public List<StrapiRelatedGuidanceItem>? RelatedGuidance { get; set; }
            [JsonPropertyName("ddatProfessionLinks")]
            public List<StrapiProfessionSidebarLink>? DdatProfessionLinks { get; set; }
            public List<StrapiHubSection>? Sections { get; set; }
        }

        private class StrapiRelatedGuidanceItem
        {
            public int Order { get; set; }
            [JsonPropertyName("external_link")]
            public StrapiExternalLinkRef? External_Link { get; set; }
        }

        private class StrapiExternalLinkRef
        {
            public string? Title { get; set; }
            public string? Url { get; set; }
        }

        private class StrapiProfessionSidebarLink
        {
            public string? Title { get; set; }
            public string? Url { get; set; }
            public int Order { get; set; }
        }

        private class StrapiHubSection
        {
            public string? Title { get; set; }
            public string? Body { get; set; }
            public int Order { get; set; }
        }

        private class StrapiMedia
        {
            public string? Url { get; set; }
            public string? AlternativeText { get; set; }
        }

        private class StrapiPhaseSummary
        {
            public string? Title { get; set; }
            public string? Slug { get; set; }
            public string? Hint { get; set; }
            public string? Summary { get; set; }
            public int Sequence { get; set; }
            public string? Duration { get; set; }
            public string? Purpose { get; set; }
            [JsonPropertyName("teamSize")]
            public string? TeamSize { get; set; }
            [JsonPropertyName("trackTags")]
            public List<StrapiTagsTrackRef>? TrackTags { get; set; }
            public StrapiPhaseAssurance? Assurance { get; set; }
            public List<StrapiPhaseActivity>? Activities { get; set; }
        }

        private class StrapiTagsTrackRef
        {
            public string? Title { get; set; }
            public string? Slug { get; set; }
        }

        private class StrapiPhase
        {
            public string? Title { get; set; }
            public string? Slug { get; set; }
            public int Sequence { get; set; }
            public string? Hint { get; set; }
            public string? Summary { get; set; }
            public string? Purpose { get; set; }
            public string? Intent { get; set; }
            public string? Duration { get; set; }
            [JsonPropertyName("teamSize")]
            public string? TeamSize { get; set; }
            public bool? IsActive { get; set; }
            public StrapiSlugRef? Lifecycle { get; set; }
            [JsonPropertyName("trackTags")]
            public List<StrapiTagsTrackRef>? TrackTags { get; set; }
            public List<StrapiPhaseActivity>? Activities { get; set; }
            [JsonPropertyName("relatedGuidance")]
            public List<StrapiPhaseRelatedLinkItem>? Related_Guidance { get; set; }
            [JsonPropertyName("relatedStandards")]
            public List<StrapiPhaseRelatedLinkItem>? Related_Standards { get; set; }
            [JsonPropertyName("relatedTools")]
            public List<StrapiPhaseRelatedLinkItem>? Related_Tools { get; set; }
            [JsonPropertyName("relatedTraining")]
            public List<StrapiPhaseRelatedLinkItem>? Related_Training { get; set; }
            public List<StrapiPhaseRequirement>? Mandatory_Requirements { get; set; }
            public List<StrapiPhaseOutput>? Outputs { get; set; }
            public StrapiPhaseAssurance? Assurance { get; set; }
        }

        private class StrapiPhaseRelatedLinkItem
        {
            [JsonPropertyName("external_link")]
            public StrapiExternalLinkRef? External_Link { get; set; }
        }

        private class StrapiPhaseActivity
        {
            public string? Title { get; set; }
            [JsonPropertyName("shortDescription")]
            public string? Short_Description { get; set; }
            public string? Description { get; set; }
            [JsonPropertyName("guidanceContent")]
            public string? Guidance_Content { get; set; }
            public int Order { get; set; }
            [JsonPropertyName("professionTags")]
            public List<StrapiTagsProfessionRef>? Profession_Tags { get; set; }
            [JsonPropertyName("trackTag")]
            public StrapiTagsTrackRef? Track_Tag { get; set; }
            [JsonPropertyName("related_content")]
            public List<StrapiSlugRef>? Related_Content { get; set; }
            public List<StrapiPhaseActivityResource>? Resources { get; set; }
        }

        private class StrapiTagsProfessionRef
        {
            public string? Title { get; set; }
        }

        private class StrapiPhaseActivityResource
        {
            [JsonPropertyName("resourceType")]
            public string? Resource_Type { get; set; }
            public string? Title { get; set; }
            public string? Description { get; set; }
            public string? Url { get; set; }
            public int Order { get; set; }
            public StrapiMedia? Media { get; set; }
        }

        private class StrapiPhaseRequirement
        {
            public string? Requirement_Title { get; set; }
            public string? Description { get; set; }
            public string? Evidence_Examples { get; set; }
            public StrapiSlugRef? Related_Standard { get; set; }
        }

        private class StrapiPhaseOutput
        {
            public string? Output_Name { get; set; }
            public string? Description { get; set; }
            public StrapiSlugRef? Template_Link { get; set; }
            public bool? Is_Mandatory { get; set; }
        }

        private class StrapiPhaseAssurance
        {
            public string? Assurance_Type { get; set; }
            public string? Description { get; set; }
            public string? Governance_Body { get; set; }
            public StrapiSlugRef? Preparation_Guidance { get; set; }
        }

        private class StrapiCollection
        {
            [JsonPropertyName("title")]
            public string? Title { get; set; }
            [JsonPropertyName("metaDescription")]
            public string? MetaDescription { get; set; }
            [JsonPropertyName("slug")]
            public string? Slug { get; set; }
            [JsonPropertyName("body")]
            public string? Body { get; set; }
            [JsonPropertyName("showLastReviewedDateOnPage")]
            public bool? ShowLastReviewedDateOnPage { get; set; }
            [JsonPropertyName("lastReviewedDate")]
            public string? LastReviewedDate { get; set; }
            [JsonConverter(typeof(StrapiContentOwnerRefConverter))]
            [JsonPropertyName("contentOwner")]
            public StrapiContentOwnerRef? ContentOwner { get; set; }
            [JsonPropertyName("applicableProfessions")]
            public List<StrapiTagsProfession>? ApplicableProfessions { get; set; }
            [JsonPropertyName("collection_sections")]
            public List<StrapiCollectionSection>? Collection_Sections { get; set; }
            [JsonPropertyName("relatedContent")]
            public List<StrapiRelatedContent>? RelatedContent { get; set; }
            [JsonConverter(typeof(StrapiRelatedFilesConverter))]
            [JsonPropertyName("relatedFiles")]
            public List<StrapiFileItem>? RelatedFiles { get; set; }
        }

        private class StrapiCollectionSection
        {
            [JsonPropertyName("title")]
            public string? Title { get; set; }
            [JsonPropertyName("summary")]
            public string? Summary { get; set; }
            [JsonPropertyName("order")]
            public int Order { get; set; }
            [JsonPropertyName("items")]
            public List<StrapiSectionLinkItem>? Items { get; set; }
        }

        private class StrapiSectionLinkItem
        {
            [JsonPropertyName("type")]
            public string? Type { get; set; }
            [JsonPropertyName("linkType")]
            public string? LinkType { get; set; }
            [JsonPropertyName("title")]
            public string? Title { get; set; }
            [JsonPropertyName("slug")]
            public string? Slug { get; set; }
            [JsonPropertyName("metaDescription")]
            public string? MetaDescription { get; set; }
            [JsonPropertyName("url")]
            public string? Url { get; set; }
            [JsonPropertyName("newTab")]
            public bool NewTab { get; set; }
            [JsonPropertyName("externalLink")]
            public bool ExternalLink { get; set; }
            [JsonPropertyName("priorityInGroup")]
            public bool PriorityInGroup { get; set; }
            [JsonPropertyName("grade")]
            public string? Grade { get; set; }
        }

        private class StrapiJobSpecification
        {
            public string? Title { get; set; }
            public string? Slug { get; set; }
            public string? Grade { get; set; }
            public string? RoleDescription { get; set; }
            public string? Skills { get; set; }
            public bool EnableWordDocDownload { get; set; }
            public StrapiJobSpecificationProfession? Profession { get; set; }
            [JsonPropertyName("siblingJobSpecifications")]
            public List<StrapiJobSpecificationSibling>? SiblingJobSpecifications { get; set; }
        }

        private class StrapiJobSpecificationProfession
        {
            [JsonPropertyName("title")]
            public string? Title { get; set; }
            [JsonPropertyName("slug")]
            public string? Slug { get; set; }
            [JsonPropertyName("plural")]
            public string? Plural { get; set; }
            [JsonPropertyName("professionDescription")]
            public string? ProfessionDescription { get; set; }
        }

        private class StrapiJobSpecificationSibling
        {
            public string? Title { get; set; }
            public string? Slug { get; set; }
            public string? Grade { get; set; }
        }

        private class StrapiDetailedGuidePageRef
        {
            public string? Title { get; set; }
            public string? Slug { get; set; }
            public string? MetaDescription { get; set; }
            [JsonPropertyName("detailed_guide")]
            public StrapiSlugRef? Detailed_Guide { get; set; }
        }

        private class StrapiExternalLink
        {
            public string? Title { get; set; }
            public string? Url { get; set; }
            [JsonPropertyName("newTab")]
            public bool NewTab { get; set; }
            [JsonPropertyName("externalLink")]
            public bool? ExternalLink { get; set; }
            public string? Description { get; set; }
        }

        private class StrapiArticle
        {
            public int Id { get; set; }
            public string? DocumentId { get; set; }
            public string? Slug { get; set; }
            public string? Title { get; set; }
            public string? MetaDescription { get; set; }
            public string? Body { get; set; }
            public string? Author { get; set; }
            public string? PublishedFrom { get; set; }
            public string? PublishedTo { get; set; }
            [JsonPropertyName("leadImage")]
            public StrapiMedia? LeadImage { get; set; }
            [JsonPropertyName("leadImageUrl")]
            public string? LeadImageUrl { get; set; }
            [JsonPropertyName("leadImageAlt")]
            public string? LeadImageAlt { get; set; }
            [JsonPropertyName("leadImageAlternativeText")]
            public string? LeadImageAlternativeText { get; set; }
        }

        private class StrapiDetailedGuide
        {
            public string? Title { get; set; }
            public string? Slug { get; set; }
            public string? MetaDescription { get; set; }
            public string? Body { get; set; }
            [JsonPropertyName("overrideOverviewTitle")]
            public string? OverrideOverviewTitle { get; set; }
            [JsonPropertyName("hideContentsOnPrimaryPage")]
            public bool? HideContentsOnPrimaryPage { get; set; }
            [JsonPropertyName("showGuidePagesOnRight")]
            public bool? ShowGuidePagesOnRight { get; set; }
            public StrapiCollectionRef? Collection { get; set; }
            [JsonConverter(typeof(StrapiContentOwnerRefConverter))]
            [JsonPropertyName("contentOwner")]
            public StrapiContentOwnerRef? ContentOwner { get; set; }
            public List<StrapiDetailedGuidePageSummary>? Detailed_Guide_Pages { get; set; }
            public List<StrapiRelatedContent>? RelatedContent { get; set; }
            [JsonConverter(typeof(StrapiTagRefListConverter))]
            [JsonPropertyName("applicablePhases")]
            public List<StrapiTagRef>? ApplicablePhases { get; set; }
            [JsonPropertyName("applicableProfessions")]
            public List<StrapiTagsProfession>? ApplicableProfessions { get; set; }
            [JsonPropertyName("showLastReviewedDateOnPage")]
            public bool? ShowLastReviewedDateOnPage { get; set; }
            [JsonPropertyName("lastReviewedDate")]
            public string? LastReviewedDate { get; set; }
            [JsonPropertyName("showOwnerOnPage")]
            public bool? ShowOwnerOnPage { get; set; }
            [JsonPropertyName("showApplicablePhasesOnPage")]
            public bool? ShowApplicablePhasesOnPage { get; set; }
            [JsonPropertyName("showApplicableProfessionsOnPage")]
            public bool? ShowApplicableProfessionsOnPage { get; set; }
            [JsonConverter(typeof(StrapiRelatedFilesConverter))]
            [JsonPropertyName("relatedFiles")]
            public List<StrapiFileItem>? RelatedFiles { get; set; }
            [JsonPropertyName("customCSS")]
            public string? CustomCss { get; set; }
            [JsonPropertyName("customJS")]
            public string? CustomJs { get; set; }
        }

        private class StrapiTagsProfession
        {
            public string? Title { get; set; }
            public string? Slug { get; set; }
            [JsonPropertyName("plural")]
            public string? Plural { get; set; }
        }

        private class StrapiDetailedGuidePage
        {
            public string? Title { get; set; }
            public string? Slug { get; set; }
            public string? MetaDescription { get; set; }
            public string? BeforeContents { get; set; }
            public string? Body { get; set; }
            public bool? HideTitleAndDescription { get; set; }
            public bool? HideContents { get; set; }
            public bool? HideGuidePagesNav { get; set; }
            [JsonConverter(typeof(StrapiTagRefListConverter))]
            [JsonPropertyName("applicablePhases")]
            public List<StrapiTagRef>? ApplicablePhases { get; set; }
            [JsonPropertyName("applicableProfessions")]
            public List<StrapiTagsProfession>? ApplicableProfessions { get; set; }
            [JsonPropertyName("Section")]
            public List<StrapiDetailedGuidePageSection>? Section { get; set; }
            public StrapiDetailedGuideRef? Detailed_Guide { get; set; }
            public List<StrapiRelatedContent>? RelatedContent { get; set; }
            [JsonConverter(typeof(StrapiRelatedFilesConverter))]
            [JsonPropertyName("relatedFiles")]
            public List<StrapiFileItem>? RelatedFiles { get; set; }
            [JsonPropertyName("showLastReviewedDateOnPage")]
            public bool? ShowLastReviewedDateOnPage { get; set; }
            [JsonPropertyName("lastReviewedDate")]
            public string? LastReviewedDate { get; set; }
        }

        private class StrapiDetailedGuidePageSection
        {
            public string? Title { get; set; }
            public string? Group { get; set; }
            public string? Body { get; set; }
            [JsonPropertyName("contentModules")]
            public List<StrapiContentEntryListItem>? ContentModules { get; set; }
        }

        private class StrapiDetailedGuideSummary
        {
            public string? Title { get; set; }
            public string? Slug { get; set; }
            public string? MetaDescription { get; set; }
        }

        private class StrapiDetailedGuidePageSummary
        {
            public string? Title { get; set; }
            public string? Slug { get; set; }
            public string? MetaDescription { get; set; }
            [JsonConverter(typeof(StrapiTagRefListConverter))]
            [JsonPropertyName("applicablePhases")]
            public List<StrapiTagRef>? ApplicablePhases { get; set; }
            [JsonPropertyName("applicableProfessions")]
            public List<StrapiTagsProfession>? ApplicableProfessions { get; set; }
        }

        private class StrapiCollectionRef
        {
            public string? Title { get; set; }
            public string? Slug { get; set; }
        }

        private class StrapiDetailedGuideRef
        {
            public string? Title { get; set; }
            public string? Slug { get; set; }
            public string? MetaDescription { get; set; }
            [JsonPropertyName("overrideOverviewTitle")]
            public string? OverrideOverviewTitle { get; set; }
            [JsonPropertyName("showLastReviewedDateOnPage")]
            public bool? ShowLastReviewedDateOnPage { get; set; }
            [JsonPropertyName("lastReviewedDate")]
            public string? LastReviewedDate { get; set; }
            [JsonPropertyName("hideContentsOnPrimaryPage")]
            public bool? HideContentsOnPrimaryPage { get; set; }
            [JsonPropertyName("showGuidePagesOnRight")]
            public bool? ShowGuidePagesOnRight { get; set; }
            public StrapiCollectionRef? Collection { get; set; }
            [JsonConverter(typeof(StrapiContentOwnerRefConverter))]
            [JsonPropertyName("contentOwner")]
            public StrapiContentOwnerRef? ContentOwner { get; set; }
            [JsonConverter(typeof(StrapiTagRefListConverter))]
            [JsonPropertyName("applicablePhases")]
            public List<StrapiTagRef>? ApplicablePhases { get; set; }
            [JsonConverter(typeof(StrapiTagRefListConverter))]
            [JsonPropertyName("applicableProfessions")]
            public List<StrapiTagRef>? ApplicableProfessions { get; set; }
            [JsonPropertyName("showOwnerOnPage")]
            public bool? ShowOwnerOnPage { get; set; }
            [JsonPropertyName("showApplicablePhasesOnPage")]
            public bool? ShowApplicablePhasesOnPage { get; set; }
            [JsonPropertyName("showApplicableProfessionsOnPage")]
            public bool? ShowApplicableProfessionsOnPage { get; set; }
            [JsonPropertyName("customCSS")]
            public string? CustomCss { get; set; }
            [JsonPropertyName("customJS")]
            public string? CustomJs { get; set; }
            public List<StrapiDetailedGuidePageSummary>? Detailed_Guide_Pages { get; set; }
        }

        private class StrapiRelatedContent
        {
            public string? Header { get; set; }
            public string? Content { get; set; }
        }

        private class StrapiNavigationItem
        {
            public int Id { get; set; }
            public string? Title { get; set; }
            public int Order { get; set; }
            public string? ExternalUrl { get; set; }
            public StrapiNavSlugRef? Collection { get; set; }
            public StrapiNavSlugRef? Detailed_Guide { get; set; }
            public List<StrapiNavigationItem>? Children { get; set; }
        }

        private class StrapiNavSlugRef
        {
            public string? Slug { get; set; }
        }

        private class StrapiDocumentationSection
        {
            public int Id { get; set; }
            public string? Title { get; set; }
            public string? Slug { get; set; }
            public int Order { get; set; }
            public bool Enabled { get; set; }
            public List<StrapiDocumentationSummary>? Documentations { get; set; }
        }

        private class StrapiDocumentationSummary
        {
            public int Id { get; set; }
            public string? Title { get; set; }
            public string? Slug { get; set; }
        }

        private class StrapiDocumentation
        {
            public int Id { get; set; }
            public string? Title { get; set; }
            public string? Slug { get; set; }
            public string? MetaDescription { get; set; }
            public string? Body { get; set; }
            [JsonPropertyName("lastReviewedDate")]
            public string? LastReviewedDate { get; set; }
            [JsonPropertyName("documentationSection")]
            public StrapiDocumentationSectionRef? DocumentationSection { get; set; }
        }

        private class StrapiDocumentationSectionRef
        {
            public string? Slug { get; set; }
            public string? Title { get; set; }
        }

        private class StrapiHtmlPage
        {
            public string? Title { get; set; }
            public string? MetaDescription { get; set; }
            public string? Slug { get; set; }
            public string? Html { get; set; }
            public string? Css { get; set; }
            public string? Js { get; set; }
        }

        private class StrapiPageNotification
        {
            public string? Title { get; set; }
            public JsonElement? Message { get; set; }
        }

        private class StrapiTagRef
        {
            public string? Slug { get; set; }
            public string? Title { get; set; }
            [JsonPropertyName("plural")]
            public string? Plural { get; set; }
        }

        /// <summary>Converter for contentOwner relation: { data: { attributes: { title, informationPage: { data: { attributes: { urlToRedirectTo } } } } } }.</summary>
        private sealed class StrapiContentOwnerRefConverter : JsonConverter<StrapiContentOwnerRef?>
        {
            public override StrapiContentOwnerRef? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Null) return null;
                if (reader.TokenType != JsonTokenType.StartObject) return null;
                using var doc = JsonDocument.ParseValue(ref reader);
                var root = doc.RootElement;
                JsonElement attrs;
                if (root.TryGetProperty("data", out var data))
                {
                    if (data.ValueKind == JsonValueKind.Null || data.ValueKind == JsonValueKind.Undefined) return null;
                    attrs = data.TryGetProperty("attributes", out var a) ? a : data;
                }
                else
                    attrs = root;
                var title = attrs.TryGetProperty("title", out var t) ? t.GetString() : null;
                string? redirectUrl = null;
                if (attrs.TryGetProperty("informationPage", out var infoPage))
                {
                    var infoData = infoPage.ValueKind == JsonValueKind.Object && infoPage.TryGetProperty("data", out var id) ? id : infoPage;
                    if (infoData.ValueKind == JsonValueKind.Object)
                    {
                        var infoAttrs = infoData.TryGetProperty("attributes", out var ia) ? ia : infoData;
                        if (infoAttrs.TryGetProperty("urlToRedirectTo", out var urlEl))
                            redirectUrl = urlEl.GetString();
                    }
                }
                if (title == null && redirectUrl == null) return null;
                return new StrapiContentOwnerRef { Title = title, RedirectUrl = redirectUrl };
            }

            public override void Write(Utf8JsonWriter writer, StrapiContentOwnerRef? value, JsonSerializerOptions options) =>
                throw new NotImplementedException();
        }

        private class StrapiContentOwnerRef
        {
            public string? Title { get; set; }
            public string? RedirectUrl { get; set; }
        }

        /// <summary>Converter for single relation that may be { data: { attributes: { slug, title } } } or { slug, title }.</summary>
        private sealed class StrapiTagRefSingleConverter : JsonConverter<StrapiTagRef?>
        {
            public override StrapiTagRef? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Null) return null;
                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    using var doc = JsonDocument.ParseValue(ref reader);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("data", out var data))
                    {
                        if (data.ValueKind == JsonValueKind.Null || data.ValueKind == JsonValueKind.Undefined) return null;
                        var attrs = data.TryGetProperty("attributes", out var a) ? a : data;
                        return new StrapiTagRef
                        {
                            Slug = attrs.TryGetProperty("slug", out var slugEl) ? slugEl.GetString() : null,
                            Title = attrs.TryGetProperty("title", out var titleEl) ? titleEl.GetString() : null,
                            Plural = attrs.TryGetProperty("plural", out var pluralEl) ? pluralEl.GetString() : null
                        };
                    }
                    return new StrapiTagRef
                    {
                        Slug = root.TryGetProperty("slug", out var slugEl2) ? slugEl2.GetString() : null,
                        Title = root.TryGetProperty("title", out var titleEl2) ? titleEl2.GetString() : null,
                        Plural = root.TryGetProperty("plural", out var pluralEl2) ? pluralEl2.GetString() : null
                    };
                }
                return null;
            }

            public override void Write(Utf8JsonWriter writer, StrapiTagRef? value, JsonSerializerOptions options) =>
                throw new NotImplementedException();
        }

        private class StrapiContentListItem
        {
            public int Id { get; set; }
            public string? DocumentId { get; set; }
            public string? Title { get; set; }
            public string? MetaDescription { get; set; }
            public string? Slug { get; set; }
            public StrapiSlugRef? Collection { get; set; }
            [JsonConverter(typeof(StrapiTagRefSingleConverter))]
            [JsonPropertyName("phaseTag")]
            public StrapiTagRef? PhaseTag { get; set; }
            [JsonConverter(typeof(StrapiTagRefListConverter))]
            [JsonPropertyName("applicablePhases")]
            public List<StrapiTagRef>? ApplicablePhases { get; set; }
            [JsonConverter(typeof(StrapiTagRefListConverter))]
            [JsonPropertyName("applicableProfessions")]
            public List<StrapiTagRef>? ApplicableProfessions { get; set; }
        }

        private class StrapiDetailedGuidePageListItem
        {
            public string? Title { get; set; }
            public string? MetaDescription { get; set; }
            public string? Slug { get; set; }
            public StrapiDetailedGuideRefForList? Detailed_Guide { get; set; }
            [JsonConverter(typeof(StrapiTagRefListConverter))]
            [JsonPropertyName("applicablePhases")]
            public List<StrapiTagRef>? ApplicablePhases { get; set; }
            [JsonConverter(typeof(StrapiTagRefListConverter))]
            [JsonPropertyName("applicableProfessions")]
            public List<StrapiTagRef>? ApplicableProfessions { get; set; }
        }

        private class StrapiDetailedGuideRefForList
        {
            public string? Slug { get; set; }
            public StrapiSlugRef? Collection { get; set; }
        }

        private class StrapiContentEntryListItem
        {
            public string? Title { get; set; }
            public string? Slug { get; set; }
            public string? Summary { get; set; }
            public string? Body { get; set; }
            public string? EntryType { get; set; }
            public string? Strength { get; set; }
            public string? Priority { get; set; }
            public bool? LegalRequirement { get; set; }
            public string? Notes { get; set; }
            [JsonConverter(typeof(StrapiTagRefListConverter))]
            [JsonPropertyName("phases")]
            public List<StrapiTagRef>? Phases { get; set; }
            [JsonConverter(typeof(StrapiTagRefListConverter))]
            [JsonPropertyName("roles")]
            public List<StrapiTagRef>? Roles { get; set; }
            [JsonPropertyName("links")]
            public List<StrapiExternalLink>? Links { get; set; }
            [JsonPropertyName("detailedGuides")]
            public List<StrapiSlugRef>? DetailedGuides { get; set; }
            [JsonPropertyName("collections")]
            public List<StrapiSlugRef>? Collections { get; set; }
        }

        private class StrapiServiceStandard
        {
            public string? Title { get; set; }
            public int? Point { get; set; }
            public string? Slug { get; set; }
            public string? Description { get; set; }
            public string? Body { get; set; }
            [JsonPropertyName("Section")]
            public List<StrapiServiceStandardSection>? Section { get; set; }
        }

        private class StrapiServiceStandardSection
        {
            public string? Title { get; set; }
            public string? Description { get; set; }
            [JsonPropertyName("content_entries")]
            public List<StrapiContentEntryListItem>? ContentEntries { get; set; }
        }

        private class StrapiPhaseListItem
        {
            public string? Title { get; set; }
            public string? Slug { get; set; }
            public string? Summary { get; set; }
        }
    }
}
