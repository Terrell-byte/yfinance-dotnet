using System.Net.Http;
using System.Text.Json;
using YFinance.Core.Entities;
using YFinance.Core.Interfaces;
using YFinance.Yahoo;

namespace YFinance.Yahoo.Services;

public class InfoService : IInfoService
{
    private readonly YahooClient _yahooClient;

    public InfoService(YahooClient yahooClient)
    {
        _yahooClient = yahooClient;
    }

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