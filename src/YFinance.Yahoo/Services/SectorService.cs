using System.Text.Json;
using System.Collections.Generic;
using YFinance.Yahoo.Interfaces;
using YFinance.Yahoo.Entities;

namespace YFinance.Yahoo.Services;

public class SectorService : ISectorService
{
    private readonly IYahooClient _yahooClient;

    public SectorService(IYahooClient yahooClient) => _yahooClient = yahooClient;

/// <summary>
/// Valid sectors:
/// "Technology",
/// "Financial Services",
/// "Consumer Cyclical",
/// "Communication Services",
/// "Healthcare",
/// "Industrials",
/// "Consumer Defensive",
/// "Energy",
/// "Basic Materials",
/// "Real Estate",
/// "Utilities"
/// </summary>
    public async Task<Sector[]> GetSectorsAsync(string[] sectors, CancellationToken ct = default)
    {
        var crumb = await _yahooClient.GetCrumbAsync(ct);
        var httpClient = _yahooClient.GetClient();
        var results = new List<Sector>();

        foreach (var sectorName in sectors)
        {
            try
            {
                var sectorKey = ConvertToSectorKey(sectorName);
                var response = await httpClient.GetAsync(
                    $"https://query1.finance.yahoo.com/v1/finance/sectors/{sectorKey}?formatted=true&withReturns=true&lang=en-US&region=US&crumb={crumb}",
                    cancellationToken: ct);
                
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(content);
                
                var data = doc.RootElement.GetProperty("data");
                
                var sector = new Sector
                {
                    name = data.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : sectorName,
                    companyCount = data.TryGetProperty("overview", out var overview) &&
                                   overview.TryGetProperty("companiesCount", out var companiesCount) 
                        ? companiesCount.GetInt32() 
                        : null,
                    dailyReturn = data.TryGetProperty("performance", out var performance) &&
                                  performance.TryGetProperty("regMarketChangePercent", out var dailyChange) &&
                                  dailyChange.TryGetProperty("raw", out var dailyRaw)
                        ? dailyRaw.GetDecimal()
                        : null,
                    annualReturn = data.TryGetProperty("performance", out var perf) &&
                                   perf.TryGetProperty("ytdChangePercent", out var ytdChange) &&
                                   ytdChange.TryGetProperty("raw", out var ytdRaw)
                        ? ytdRaw.GetDecimal()
                        : null
                };
                
                results.Add(sector);
            }
            catch
            {
                // If a sector fails, add it with null values but keep the name
                results.Add(new Sector { name = sectorName });
            }
        }

        return results.ToArray();
    }

    private static string ConvertToSectorKey(string sectorName)
    {
        // Convert sector name to URL-friendly key format
        // Examples: "Real Estate" -> "real-estate", "Health Care" -> "health-care"
        var key = sectorName.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("&", "and");
        return key;
    }
}
