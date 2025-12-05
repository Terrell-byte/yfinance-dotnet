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
        var serviceProvider = services.BuildServiceProvider();
    }
}
