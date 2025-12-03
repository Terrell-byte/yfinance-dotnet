using System.Net.Http;
using System.Text.Json;
using YFinance.Yahoo.Entities;
using YFinance.Yahoo.Interfaces;

namespace YFinance.Yahoo.Services;

/// <summary>
/// Service implementation for retrieving market-wide stock data from Yahoo Finance.
/// Provides access to all US stocks with their market data.
/// </summary>
public class MarketService : IMarketService
{
    private readonly IYahooClient _yahooClient;
    
    /// <summary>
    /// Initializes a new instance of the MarketService class.
    /// </summary>
    /// <param name="yahooClient">The Yahoo Finance client used for making API requests.</param>
    public MarketService(IYahooClient yahooClient) => _yahooClient = yahooClient;

    /// <summary>
    /// Retrieves all US stocks from the market with their longName, ask, and regularMarketChangePercent.
    /// </summary>
    /// <param name="count">Maximum number of stocks to retrieve. If null, attempts to get all available stocks.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// An array of <see cref="Quote"/> objects containing market data for US stocks.
    /// </returns>
    /// <exception cref="HttpRequestException">Thrown when the API request fails or returns an error status code.</exception>
    public async Task<Quote[]> GetAllUSStocksAsync(int? count = null, CancellationToken ct = default)
    {
        var crumb = await _yahooClient.GetCrumbAsync(ct);
        var httpClient = _yahooClient.GetClient();

        // Use trending endpoint with higher count, or use screener endpoint
        // For now, using trending endpoint with configurable count
        var countParam = count?.ToString() ?? "5000"; // Try to get a large number
        var fields = "logoUrl,longName,shortName,regularMarketChange,regularMarketChangePercent,regularMarketPrice,ask";
        
        var url = $"https://query1.finance.yahoo.com/v1/finance/trending/US?count={countParam}&fields={Uri.EscapeDataString(fields)}&format=true&useQuotes=true&quoteType=ALL&lang=en-US&region=US&crumb={crumb}";
        
        var response = await httpClient.GetAsync(url, cancellationToken: ct);
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(content);
        
        // Navigate through the response structure: finance.result[0].quotes[]
        var finance = doc.RootElement.GetProperty("finance");
        var result = finance.GetProperty("result");
        var quotesArray = result[0].GetProperty("quotes");
        
        return quotesArray.EnumerateArray().Select(item => new Quote()
        {
            ticker = item.TryGetProperty("symbol", out var s) ? s.GetString() : null,
            name = item.TryGetProperty("longName", out var n) ? n.GetString() : null,
            ask = ExtractDecimal(item, "ask") ?? ExtractDecimal(item, "regularMarketPrice"),
            percentageChange = ExtractDecimal(item, "regularMarketChangePercent"),
        }).ToArray();
    }
    
    private static decimal? ExtractDecimal(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var prop) || prop.ValueKind == JsonValueKind.Null)
            return null;
            
        // If it's already a number, return it directly
        if (prop.ValueKind == JsonValueKind.Number)
            return prop.GetDecimal();
            
        // If it's an object, try to get the "raw" property
        if (prop.ValueKind == JsonValueKind.Object && prop.TryGetProperty("raw", out var raw))
        {
            if (raw.ValueKind == JsonValueKind.Number)
                return raw.GetDecimal();
        }
        
        return null;
    }
}