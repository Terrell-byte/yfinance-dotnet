using System.Net.Http;
using System.Text.Json;
using YFinance.Yahoo.Entities;
using YFinance.Yahoo.Interfaces;

namespace YFinance.Yahoo.Services;

/// <summary>
/// Service implementation for retrieving real-time stock quotes from Yahoo Finance.
/// Provides current market data including prices, bid/ask spreads, and percentage changes.
/// </summary>
public class QuoteService : IQuoteService
{
    private readonly IYahooClient _yahooClient;
    
    /// <summary>
    /// Initializes a new instance of the QuoteService class.
    /// </summary>
    /// <param name="yahooClient">The Yahoo Finance client used for making API requests.</param>
    public QuoteService(IYahooClient yahooClient) => _yahooClient = yahooClient;

    /// <summary>
    /// Retrieves current market quotes for the specified stock tickers.
    /// Returns real-time pricing data including bid, ask, open price, and percentage change.
    /// </summary>
    /// <param name="tickers">Array of stock ticker symbols (e.g., "AAPL", "MSFT", "GOOGL").</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// An array of <see cref="Quote"/> objects containing current market data for each ticker.
    /// The array may contain fewer items than requested if some tickers are invalid or not found.
    /// </returns>
    /// <exception cref="HttpRequestException">Thrown when the API request fails or returns an error status code.</exception>
    /// <example>
    /// <code>
    /// var quoteService = serviceProvider.GetRequiredService&lt;IQuoteService&gt;();
    /// var quotes = await quoteService.GetQuoteAsync(new[] { "AAPL", "MSFT" });
    /// foreach (var quote in quotes)
    /// {
    ///     Console.WriteLine($"{quote.ticker}: ${quote.ask} (Change: {quote.percentageChange:P})");
    /// }
    /// </code>
    /// </example>
    public async Task<Quote[]> GetQuoteAsync(string[] tickers, CancellationToken ct = default)
    {
        var crumb = await _yahooClient.GetCrumbAsync(ct);
        var httpClient = _yahooClient.GetClient();

        var response = await httpClient.GetAsync(
            $"https://query1.finance.yahoo.com/v7/finance/quote?symbols={string.Join(",", tickers)}&fields=regularMarketOpen,longName,ask,bid,regularMarketChangePercent,regularMarketPrice&crumb={crumb}",
            cancellationToken: ct);

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(content);
        var quoteData = doc.RootElement.GetProperty("quoteResponse").GetProperty("result");
        
        return quoteData.EnumerateArray().Select(item => new Quote()
        {
            ticker = item.TryGetProperty("symbol", out var s) ? s.GetString() : null,
            name = item.TryGetProperty("longName", out var n) ? n.GetString() : null,
            open = item.TryGetProperty("regularMarketOpen", out var o) ? o.GetDecimal() : null,
            price = item.TryGetProperty("regularMarketPrice", out var p) ? p.GetDecimal() : null,
            ask = item.TryGetProperty("ask", out var a) ? a.GetDecimal() : null,
            bid = item.TryGetProperty("bid", out var b) ? b.GetDecimal() : null,
            percentageChange = item.TryGetProperty("regularMarketChangePercent", out var pc) ? pc.GetDecimal() : null,
        }).ToArray();
    }
}