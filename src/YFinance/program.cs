using System.Text.Json;
using System.Text.Encodings.Web;
using System.Net.Http;
using System.Net;
using YFinance.Yahoo;
using YFinance.Yahoo.Entities;
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
        services.AddSingleton<ISectorService, SectorService>();
        services.AddSingleton<INewsService, NewsService>();
        var serviceProvider = services.BuildServiceProvider();

        var newsService = serviceProvider.GetRequiredService<INewsService>();
        var tags = new[]
        {
            new IndustryTag("Technology", "Semiconductors", "^YH311-latest-news")
        };

        var news = await newsService.GetIndustryNewsAsync(tags, count: 60);
        
        // Count total articles
        var totalArticles = news.sectors.Values
            .SelectMany(industries => industries.Values)
            .SelectMany(articles => articles)
            .Count();
        
        Console.WriteLine($"\nTotal articles scraped: {totalArticles}");
        
        // Serialize to JSON with proper Unicode handling (don't escape Unicode characters)
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        var json = JsonSerializer.Serialize(news, jsonOptions);
        
        Console.WriteLine("\n--- JSON Output ---");
        Console.WriteLine(json);
    }
}
