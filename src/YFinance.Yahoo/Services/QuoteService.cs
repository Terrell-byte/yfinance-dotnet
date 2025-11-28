using System.Net.Http;
using System.Text.Json;
using YFinance.Core.Entities;
using YFinance.Core.Interfaces;
using YFinance.Yahoo;

namespace YFinance.Yahoo.Services;

public class QuoteService : IQuoteService
{
    private readonly YahooClient _yahooClient;
    
    public QuoteService(YahooClient yahooClient)
    {
        _yahooClient = yahooClient;
    }
    
    public async Task<Quote> GetQuoteAsync(string ticker, CancellationToken cancellationToken = default)
    {
        var crumb = await _yahooClient.GetCrumbAsync(cancellationToken);
        var httpClient = _yahooClient.GetClient();
        
        var response = await httpClient.GetAsync(
            $"https://query1.finance.yahoo.com/v7/finance/quote?symbols={ticker}&fields=regularMarketOpen,ask,bid,regularMarketChangePercent&crumb={crumb}",
            cancellationToken
        );
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(content);

        // Extract just the quote data (the inner object)
        var quoteData = doc.RootElement.GetProperty("quoteResponse").GetProperty("result")[0];

        return new Quote()
        {
            ticker = quoteData.TryGetProperty("symbol", out var s) ? s.GetString() : ticker,
            open = quoteData.TryGetProperty("regularMarketOpen", out var o) && o.TryGetDecimal(out var openVal) ? openVal : null,
            ask = quoteData.TryGetProperty("ask", out var a) && a.TryGetDecimal(out var askVal) ? askVal : null,
            bid = quoteData.TryGetProperty("bid", out var b) && b.TryGetDecimal(out var bidVal) ? bidVal : null,
            percentageChange = quoteData.TryGetProperty("regularMarketChangePercent", out var pc) && pc.TryGetDecimal(out var percentageChangeVal) ? percentageChangeVal : null,
        };
    }
}