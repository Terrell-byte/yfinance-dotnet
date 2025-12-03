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
        services.AddSingleton<IMarketService, MarketService>();
        var serviceProvider = services.BuildServiceProvider();
        var marketService = serviceProvider.GetRequiredService<IMarketService>();
        var allStocks = await marketService.GetAllUSStocksAsync(null, CancellationToken.None);
        Console.WriteLine(JsonSerializer.Serialize(allStocks, new JsonSerializerOptions { WriteIndented = true }));
    }
}
