using System.Net.Http;
using System.Text.Json;
using YFinance.Yahoo.Entities;
using YFinance.Yahoo.Interfaces;

namespace YFinance.Yahoo.Services;

/// <summary>
/// Service implementation for retrieving stock information from Yahoo Finance.
/// Provides company details including name, exchange, industry, and sector.
/// </summary>
public class InfoService : IInfoService
{
    private readonly IYahooClient _yahooClient;
    
    /// <summary>
    /// Initializes a new instance of the InfoService class.
    /// </summary>
    /// <param name="yahooClient">The Yahoo Finance client used for making API requests.</param>
    public InfoService(IYahooClient yahooClient) => _yahooClient = yahooClient;
    
    /// <summary>
    /// Retrieves detailed information for the specified stock tickers.
    /// Returns company information including name, exchange, industry, and sector.
    /// </summary>
    /// <param name="tickers">Array of stock ticker symbols (e.g., "AAPL", "MSFT", "GOOGL").</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// An array of <see cref="Info"/> objects containing company information for each ticker.
    /// The array may contain fewer items than requested if some tickers are invalid or not found.
    /// </returns>
    /// <exception cref="HttpRequestException">Thrown when the API request fails or returns an error status code.</exception>
    /// <example>
    /// <code>
    /// var infoService = serviceProvider.GetRequiredService&lt;IInfoService&gt;();
    /// var info = await infoService.GetInfoAsync(new[] { "AAPL", "MSFT" });
    /// foreach (var company in info)
    /// {
    ///     Console.WriteLine($"{company.ticker}: {company.name} - {company.sector}");
    /// }
    /// </code>
    /// </example>
    public async Task<Info[]> GetInfoAsync(string[] tickers, CancellationToken ct = default)
    {
        var crumb = await _yahooClient.GetCrumbAsync(ct);
        var httpClient = _yahooClient.GetClient();

        var response = await httpClient.GetAsync(
            $"https://query1.finance.yahoo.com/v7/finance/quote?symbols={string.Join(",", tickers)}&fields=longName,fullExchangeName,industry,sector&crumb={crumb}",
            cancellationToken: ct);

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(content);

        var infoData = doc.RootElement.GetProperty("quoteResponse").GetProperty("result");
        
        return infoData.EnumerateArray().Select(item => new Info()
        {
            ticker = item.TryGetProperty("symbol", out var s) ? s.GetString() : null,
            name = item.TryGetProperty("longName", out var n) ? n.GetString() : null,
            exchange = item.TryGetProperty("fullExchangeName", out var e) ? e.GetString() : null,
            industry = item.TryGetProperty("industry", out var i) ? i.GetString() : null,
            sector = item.TryGetProperty("sector", out var se) ? se.GetString() : null,
        }).ToArray();
    }
}