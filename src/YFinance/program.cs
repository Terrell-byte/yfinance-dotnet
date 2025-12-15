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
        services.AddSingleton<ISectorService, SectorService>();
        var serviceProvider = services.BuildServiceProvider();
        
        // Test the SectorService
        var sectorService = serviceProvider.GetRequiredService<ISectorService>();
        var sectors = await sectorService.GetSectorsAsync(new[] 
        { 
            "Technology", 
            "Financial Services",
            "Consumer Cyclical",
            "Communication Services",
            "Healthcare",
            "Industrials",
            "Consumer Defensive",
            "Energy",
            "Basic Materials",
            "Real Estate",
            "Utilities",
        });
        
        foreach (var sector in sectors)
        {
            Console.WriteLine($"Sector: {sector.name}");
            Console.WriteLine($"  Companies: {sector.companyCount}");
            Console.WriteLine($"  Daily Return: {sector.dailyReturn:P2}");
            Console.WriteLine($"  Annual Return: {sector.annualReturn:P2}");
            Console.WriteLine();
        }
    }
}
