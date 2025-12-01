# YFinance.Yahoo

Unofficial C# client for Yahoo Finance API with dependency injection support.

## Installation

```bash
dotnet add package YFinance.Yahoo
```

## Usage with Dependency Injection

### 1. Register Services

```csharp
using Microsoft.Extensions.DependencyInjection;
using YFinance.Yahoo;
using YFinance.Yahoo.Services;
using YFinance.Yahoo.Interfaces;

var services = new ServiceCollection();

// Register YahooClient as singleton (important to avoid IP bans)
services.AddSingleton<IYahooClient, YahooClient>();

// Register services
services.AddSingleton<IInfoService, InfoService>();
services.AddSingleton<IQuoteService, QuoteService>();

var serviceProvider = services.BuildServiceProvider();
```

### 2. Use the Services

```csharp
// Get services from DI container
var infoService = serviceProvider.GetRequiredService<IInfoService>();
var quoteService = serviceProvider.GetRequiredService<IQuoteService>();

// Get stock information
var tickers = new[] { "NVDA", "AAPL", "GME" };
var info = await infoService.GetInfoAsync(tickers);
var quotes = await quoteService.GetQuoteAsync(tickers);
```

## Why Singleton?

`YahooClient` should be registered as a **singleton** to:
- Avoid creating multiple `HttpClient` instances
- Prevent IP bans from Yahoo Finance
- Reuse cookies and cached crumb tokens efficiently

## Namespaces

- `YFinance.Yahoo` - Main client implementation (`YahooClient`)
- `YFinance.Yahoo.Interfaces` - Service interfaces (`IYahooClient`, `IInfoService`, `IQuoteService`)
- `YFinance.Yahoo.Services` - Service implementations (`InfoService`, `QuoteService`)
- `YFinance.Yahoo.Entities` - Data models (`Info`, `Quote`, `History`)

