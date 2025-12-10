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
        services.AddSingleton<IQuoteService, QuoteService>();
        var serviceProvider = services.BuildServiceProvider();
        // lets test a price on a stock
        var quoteService = serviceProvider.GetRequiredService<IQuoteService>();
        var quotes = await quoteService.GetQuoteAsync(new[] { "NVO" });
        Console.WriteLine(quotes[0].ask);
        Console.WriteLine(quotes[0].bid);
        Console.WriteLine(quotes[0].price);
        Console.WriteLine(quotes[0].open);
        Console.WriteLine(quotes[0].percentageChange);
        Console.WriteLine(quotes[0].name);
        Console.WriteLine(quotes[0].ticker);
    }
}
