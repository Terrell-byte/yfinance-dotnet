using System.Text.Json;
using HtmlAgilityPack;
using YFinance.Yahoo.Entities;
using YFinance.Yahoo.Interfaces;
using System.Diagnostics;

namespace YFinance.Yahoo.Services;

public class NewsService : INewsService
{
    private static readonly TimeSpan DefaultWindow = TimeSpan.FromHours(24);
    private static readonly TimeSpan PerArticleDelay = TimeSpan.FromMilliseconds(300);

    private readonly IYahooClient _yahooClient;

    public NewsService(IYahooClient yahooClient)
    {
        _yahooClient = yahooClient;
    }

    public async Task<IndustryNewsResponse> GetIndustryNewsAsync(
        IEnumerable<IndustryTag> tags,
        int count = 60,
        TimeSpan? window = null,
        CancellationToken ct = default)
    {
        var tagList = tags?.ToArray() ?? Array.Empty<IndustryTag>();
        if (tagList.Length == 0)
        {
            return new IndustryNewsResponse();
        }

        var cutoff = DateTimeOffset.UtcNow - (window ?? DefaultWindow);
        var httpClient = _yahooClient.GetClient();
        var crumb = await _yahooClient.GetCrumbAsync(ct);

        var sectors = new Dictionary<string, Dictionary<string, List<Article>>>(StringComparer.OrdinalIgnoreCase);
        var seenLinks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var articleIdCounter = 1;

        foreach (var tag in tagList)
        {
            var feedUrl =
                $"https://finance.yahoo.com/xhr/ncp?location=US&queryRef=latestNews&serviceKey=ncp_fin&listName={Uri.EscapeDataString(tag.listName)}&lang=en-US&region=US";
            Console.WriteLine($"[DEBUG] POST to: {feedUrl}");
            
            var request = new HttpRequestMessage(HttpMethod.Post, feedUrl);
            request.Headers.Add("Referer", "https://finance.yahoo.com/");
            request.Headers.Add("Origin", "https://finance.yahoo.com");
            
            // Extract base tag for s array (remove -latest-news suffix if present)
            var baseTag = tag.listName.EndsWith("-latest-news", StringComparison.OrdinalIgnoreCase)
                ? tag.listName.Substring(0, tag.listName.Length - "-latest-news".Length)
                : tag.listName;
            
            var bodyJson = BuildPostBody(baseTag, count, crumb);
            request.Content = new StringContent(bodyJson, System.Text.Encoding.UTF8, "text/plain");
            Console.WriteLine($"[DEBUG] POST body length: {bodyJson.Length}");
            
            using var response = await httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                Console.WriteLine($"[DEBUG] Feed request failed ({response.StatusCode}) for listName={tag.listName}");
                if (errorBody.Length > 0)
                {
                    Console.WriteLine($"[DEBUG] Error body: {errorBody.Substring(0, Math.Min(500, errorBody.Length))}");
                }
                continue;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            Console.WriteLine($"[DEBUG] Response for {tag.listName}: Status={response.StatusCode}, Length={json.Length}");
            if (json.Length > 0)
            {
                Console.WriteLine($"[DEBUG] First 500 chars: {json.Substring(0, Math.Min(500, json.Length))}");
            }
            
            using var doc = JsonDocument.Parse(json);

            if (!TryGetItems(doc.RootElement, out var items))
            {
                Console.WriteLine($"[DEBUG] No items found. Root keys: {string.Join(", ", doc.RootElement.EnumerateObject().Select(p => p.Name))}");
                // Check pagination info if available
                if (doc.RootElement.TryGetProperty("data", out var data) &&
                    data.TryGetProperty("tickerStream", out var tickerStream) &&
                    tickerStream.TryGetProperty("pagination", out var pagination))
                {
                    Console.WriteLine($"[DEBUG] Pagination: {pagination}");
                }
                continue;
            }

            var itemCount = items.GetArrayLength();
            Console.WriteLine($"[DEBUG] Found {itemCount} items in stream");
            var processedCount = 0;
            foreach (var item in items.EnumerateArray())
            {
                processedCount++;
                
                // Stream items have a nested "content" object
                if (!item.TryGetProperty("content", out var content))
                {
                    Console.WriteLine($"[DEBUG] Item {processedCount}: No content property");
                    continue;
                }

                var published = GetPublishedTime(content);
                if (published is null || published < cutoff)
                {
                    Console.WriteLine($"[DEBUG] Item {processedCount}: Skipped (no publish time or too old)");
                    continue;
                }

                var link = GetClickThroughUrl(content);
                Console.WriteLine($"[DEBUG] Item {processedCount}: link={link}");

                if (string.IsNullOrWhiteSpace(link) || !seenLinks.Add(link))
                {
                    Console.WriteLine($"[DEBUG] Item {processedCount}: Skipped (no link or duplicate)");
                    continue;
                }

                var title = GetTitle(content);
                
                // Fetch and scrape article body from Yahoo Finance page
                var body = await TryFetchArticleBodyAsync(httpClient, link, ct);
                
                var articleId = articleIdCounter++;
                var article = new Article(articleId, title, body, link, published);
                Console.WriteLine($"[DEBUG] Item {processedCount}: Added article #{articleId} - {title} -> {link}");

                AddArticle(sectors, tag.sector, tag.industry, article);

                // Throttle between article fetches
                if (PerArticleDelay > TimeSpan.Zero)
                {
                    await Task.Delay(PerArticleDelay, ct);
                }
            }
        }

        return new IndustryNewsResponse { sectors = sectors };
    }

    private static void AddArticle(
        Dictionary<string, Dictionary<string, List<Article>>> sectors,
        string sector,
        string industry,
        Article article)
    {
        if (!sectors.TryGetValue(sector, out var industries))
        {
            industries = new Dictionary<string, List<Article>>(StringComparer.OrdinalIgnoreCase);
            sectors[sector] = industries;
        }

        if (!industries.TryGetValue(industry, out var articles))
        {
            articles = new List<Article>();
            industries[industry] = articles;
        }

        articles.Add(article);
    }

    private static bool TryGetItems(JsonElement root, out JsonElement items)
    {
        // Check for data.tickerStream.stream (actual NCP response structure)
        if (root.TryGetProperty("data", out var data) && 
            data.TryGetProperty("tickerStream", out var tickerStream) &&
            tickerStream.TryGetProperty("stream", out items) &&
            items.ValueKind == JsonValueKind.Array)
        {
            return true;
        }

        // Fallback to other possible structures
        if (root.TryGetProperty("data", out var data2) && data2.TryGetProperty("items", out items))
        {
            return true;
        }

        if (root.TryGetProperty("items", out items))
        {
            return true;
        }

        items = default;
        return false;
    }

    private static string? GetTitle(JsonElement content)
    {
        if (content.TryGetProperty("title", out var titleProp))
        {
            return titleProp.GetString();
        }
        return null;
    }

    private static string? GetClickThroughUrl(JsonElement content)
    {
        // Check if clickThroughUrl exists and is not null
        if (content.TryGetProperty("clickThroughUrl", out var ctu) &&
            ctu.ValueKind == JsonValueKind.Object &&
            ctu.TryGetProperty("url", out var urlProp))
        {
            return urlProp.GetString();
        }
        
        // Fallback to canonicalUrl (also check it's not null)
        if (content.TryGetProperty("canonicalUrl", out var canonical) &&
            canonical.ValueKind == JsonValueKind.Object &&
            canonical.TryGetProperty("url", out var canonicalUrlProp))
        {
            return canonicalUrlProp.GetString();
        }
        
        return null;
    }

    private static DateTimeOffset? GetPublishedTime(JsonElement content)
    {
        // NCP stream uses ISO 8601 pubDate string
        if (content.TryGetProperty("pubDate", out var pubDateProp))
        {
            var pubDateStr = pubDateProp.GetString();
            if (!string.IsNullOrWhiteSpace(pubDateStr) &&
                DateTimeOffset.TryParse(pubDateStr, out var pubDate))
            {
                return pubDate;
            }
        }

        // Fallback to displayTime
        if (content.TryGetProperty("displayTime", out var displayTimeProp))
        {
            var displayTimeStr = displayTimeProp.GetString();
            if (!string.IsNullOrWhiteSpace(displayTimeStr) &&
                DateTimeOffset.TryParse(displayTimeStr, out var displayTime))
            {
                return displayTime;
            }
        }

        // Legacy Unix timestamp fields (for other endpoints)
        if (content.TryGetProperty("providerPublishTime", out var providerTs))
        {
            return DateTimeOffset.FromUnixTimeSeconds(providerTs.GetInt64());
        }

        if (content.TryGetProperty("publishTime", out var publishTs))
        {
            return DateTimeOffset.FromUnixTimeSeconds(publishTs.GetInt64());
        }

        return null;
    }

    private static async Task<string?> TryFetchArticleBodyAsync(HttpClient httpClient, string url, CancellationToken ct)
    {
        try
        {
            using var response = await httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var html = await response.Content.ReadAsStringAsync(ct);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Extract body from bodyItems-wrapper and read-more-wrapper
            var bodyNodes = doc.DocumentNode.SelectNodes(
                "//*[contains(concat(' ', normalize-space(@class), ' '), ' bodyItems-wrapper ') or contains(concat(' ', normalize-space(@class), ' '), ' read-more-wrapper ')]");

            if (bodyNodes is null || bodyNodes.Count == 0)
            {
                return null;
            }

            var bodyParts = bodyNodes
                .Select(n => HtmlEntity.DeEntitize(n.InnerText).Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            if (bodyParts.Count == 0)
            {
                return null;
            }

            return string.Join("\n\n", bodyParts);
        }
        catch
        {
            // If scraping fails, return null (article will still have title and link)
            return null;
        }
    }

    private static string BuildPostBody(string listName, int count, string crumb)
    {
        var body = new
        {
            serviceConfig = new
            {
                count = count,
                imageTags = new[] { "168x126|1|80", "168x126|2|80" },
                s = new[] { listName },
                snippetCount = 20,
                spaceId = "1197812728",
                thumbnailSizes = new[] { "170x128" }
            },
            session = new
            {
                adblock = "0",
                app = "unknown",
                areAdsEnabled = true,
                authed = "0",
                bot = "0",
                browser = "chrome",
                bucket = new[] { "my-money-beta-v1", "yfqsp-ai-onramp-1", "transmit-prebid-mtls-ctrl", "siphon-article-test" },
                ccpa = new { warning = "", footerSequence = new[] { "terms_and_privacy", "privacy_settings" }, links = new { } },
                colo = "ir2",
                consent = new
                {
                    allowContentPersonalization = false,
                    allowCrossDeviceMapping = false,
                    allowFirstPartyAds = false,
                    allowSellPersonalInfo = true,
                    canEmbedThirdPartyContent = false,
                    canSell = true,
                    consentedVendors = Array.Empty<string>(),
                    allowAds = true,
                    allowOnlyLimitedAds = true,
                    rejectedAllConsent = true,
                    allowOnlyNonPersonalizedAds = false
                },
                device = "desktop",
                dir = "ltr",
                ecma = "modern",
                environment = "prod",
                feature = new[] { "awsCds", "disableInterstitialUpsells", "disableServiceRewrite", "disableBack2Classic" },
                gdpr = true,
                gucJurisdiction = "DK",
                intl = "us",
                isDebug = false,
                isError = false,
                isForScreenshot = false,
                isWebview = false,
                lang = "en-US",
                mode = "normal",
                network = "broadband",
                os = "windows nt",
                partner = "none",
                pnrID = "",
                region = "US",
                rmp = "0",
                searchCrumb = crumb,
                site = "finance",
                spdy = "0",
                ssl = "1",
                theme = "auto",
                time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                tpConsent = false,
                tz = "Europe/Copenhagen",
                user = new
                {
                    age = -2147483648,
                    crumb = crumb,
                    firstName = (string?)null,
                    gender = "",
                    year = 0
                },
                usercountry = "DK",
                webview = "0",
                ynet = "0",
                yrid = "0n5qmklkmckau",
                ytee = "0"
            }
        };

        return JsonSerializer.Serialize(body, new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });
    }
}

