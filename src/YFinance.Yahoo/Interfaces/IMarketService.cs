using YFinance.Yahoo.Entities;

namespace YFinance.Yahoo.Interfaces;

public interface IMarketService
{
    Task<Quote[]> GetAllUSStocksAsync(int? count = null, CancellationToken ct = default);
}
