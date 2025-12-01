using YFinance.Yahoo.Entities;

namespace YFinance.Yahoo.Interfaces;

public interface IQuoteService
{
    Task<Quote[]> GetQuoteAsync(string[] tickers, CancellationToken ct = default);
}

