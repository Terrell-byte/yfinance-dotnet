using System.Text.Json;
using System.Net.Http;
using System.Net;
using YFinance.Yahoo;
using YFinance.Yahoo.Services;
using YFinance.Yahoo.Interfaces;
using Microsoft.Extensions.DependencyInjection;

public class Program
{
    public static async Task Main(string[] args)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IYahooClient, YahooClient>();
        services.AddSingleton<IInfoService, InfoService>();
        services.AddSingleton<IQuoteService, QuoteService>();
        var serviceProvider = services.BuildServiceProvider();
        var yahooClient = serviceProvider.GetRequiredService<IYahooClient>();
        var infoService = serviceProvider.GetRequiredService<IInfoService>();
        var quoteService = serviceProvider.GetRequiredService<IQuoteService>();
        var info = await infoService.GetInfoAsync(new[] { "NVDA", "AAPL", "GME" }, CancellationToken.None);
        var quote = await quoteService.GetQuoteAsync(new[] { "NVDA", "AAPL", "GME" }, CancellationToken.None);
        Console.WriteLine(JsonSerializer.Serialize(info, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine(JsonSerializer.Serialize(quote, new JsonSerializerOptions { WriteIndented = true }));
    }
}
