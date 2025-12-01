using YFinance.Yahoo.Entities;

namespace YFinance.Yahoo.Interfaces;

public interface IInfoService
{
    Task<Info[]> GetInfoAsync(string[] tickers, CancellationToken ct = default);
}

