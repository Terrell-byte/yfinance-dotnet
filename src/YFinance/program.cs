using System.Text.Json;
using System.Net.Http;
using System.Net;
using YFinance.Yahoo;
using YFinance.Yahoo.Services;

public class Program
{
    public static async Task Main(string[] args)
    {
        var yahooClient = new YahooClient();
        var infoService = new InfoService(yahooClient);
        var info = await infoService.GetInfoAsync(new[] { "NVDA", "AAPL" }, CancellationToken.None);
        var quoteService = new QuoteService(yahooClient);
        var quote = await quoteService.GetQuoteAsync(new[] { "NVDA", "AAPL" }, CancellationToken.None);
        Console.WriteLine(JsonSerializer.Serialize(info, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine(JsonSerializer.Serialize(quote, new JsonSerializerOptions { WriteIndented = true }));
    }
}
