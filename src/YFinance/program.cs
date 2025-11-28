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
        var quoteService = new QuoteService(yahooClient);
        var quote = await quoteService.GetQuoteAsync("NVDA", CancellationToken.None);
        Console.WriteLine(JsonSerializer.Serialize(quote, new JsonSerializerOptions { WriteIndented = true }));
    }
}
