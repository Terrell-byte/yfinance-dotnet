using System.Net;
using System.Net.Http;

namespace YFinance.Yahoo;

public class YahooClient
{
    private readonly HttpClient _httpClient;
    private readonly CookieContainer _cookieContainer;
    private string? _cachedCrumb;
    private DateTime _crumbExpiry;
    
    public YahooClient()
    {
        _cookieContainer = new CookieContainer();
        var handler = new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = _cookieContainer
        };
        _httpClient = new HttpClient(handler);
        
        // Set User-Agent to look like a real browser
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    }
    
    public HttpClient GetClient() => _httpClient;
    public async Task<string> GetCrumbAsync(CancellationToken ct = default)
    {
        if (_cachedCrumb != null && _crumbExpiry > DateTime.UtcNow) 
        {
            return _cachedCrumb;
        }
        await _httpClient.GetAsync("https://fc.yahoo.com", ct);

        // Retry logic for rate limiting (429)
        const int maxRetries = 3;
        for (int i = 0; i < maxRetries; i++)
        {
            var response = await _httpClient.GetAsync("https://query2.finance.yahoo.com/v1/test/getcrumb", ct);
            
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                // Wait before retrying (exponential backoff)
                var delay = TimeSpan.FromSeconds(Math.Pow(2, i)); // 1s, 2s, 4s
                await Task.Delay(delay, ct);
                continue;
            }
            
            response.EnsureSuccessStatusCode();
            var crumb = (await response.Content.ReadAsStringAsync(ct)).Trim();
            _cachedCrumb = crumb;
            _crumbExpiry = DateTime.UtcNow.AddSeconds(50);
            
            return crumb;
        }
        
        throw new HttpRequestException("Failed to get crumb after retries due to rate limiting");
    }
}