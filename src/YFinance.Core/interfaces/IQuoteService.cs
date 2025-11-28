using YFinance.Core.Entities;

namespace YFinance.Core.Interfaces;

public interface IQuoteService
{
    Task<Quote> GetQuoteAsync(string ticker, CancellationToken cancellationToken = default);
}