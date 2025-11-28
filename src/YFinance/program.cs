using System.Text.Json;
using System.Net.Http;
using System.Net;
using YFinance.Yahoo;
using YFinance.Yahoo.Services;

public class Program
{
    public static async Task Main(string[] args)
    {
        var httpClientFactory = new YahooHttpClientFactory();
        var quoteService = new YahooQuoteService(httpClientFactory);
        var quote = await quoteService.GetQuoteAsync("AAPL", CancellationToken.None);
        Console.WriteLine(JsonSerializer.Serialize(quote, new JsonSerializerOptions { WriteIndented = true }));
    }
}
