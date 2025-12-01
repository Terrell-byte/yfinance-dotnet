using System.Net.Http;
using System.Text.Json;
using YFinance.Yahoo.Entities;
using YFinance.Yahoo.Interfaces;

namespace YFinance.Yahoo.Services;

public class QuoteService : IQuoteService
{
    private readonly IYahooClient _yahooClient;
    public QuoteService(IYahooClient yahooClient) => _yahooClient = yahooClient;

    
    public async Task<Quote[]> GetQuoteAsync(string[] tickers, CancellationToken ct = default)
    {
        var crumb = await _yahooClient.GetCrumbAsync(ct);
        var httpClient = _yahooClient.GetClient();

        var response = await httpClient.GetAsync(
            $"https://query1.finance.yahoo.com/v7/finance/quote?symbols={string.Join(",", tickers)}&fields=regularMarketOpen,ask,bid,regularMarketChangePercent&crumb={crumb}",
            cancellationToken: ct);

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(content);
        var quoteData = doc.RootElement.GetProperty("quoteResponse").GetProperty("result");
        
        return quoteData.EnumerateArray().Select(item => new Quote()
        {
            ticker = item.TryGetProperty("symbol", out var s) ? s.GetString() : null,
            open = item.TryGetProperty("regularMarketOpen", out var o) ? o.GetDecimal() : null,
            ask = item.TryGetProperty("ask", out var a) ? a.GetDecimal() : null,
            bid = item.TryGetProperty("bid", out var b) ? b.GetDecimal() : null,
            percentageChange = item.TryGetProperty("regularMarketChangePercent", out var pc) ? pc.GetDecimal() : null,
        }).ToArray();
    }
}