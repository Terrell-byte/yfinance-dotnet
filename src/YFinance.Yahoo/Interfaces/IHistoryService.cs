using YFinance.Yahoo.Entities;

namespace YFinance.Yahoo.Interfaces;

public interface IHistoryService
{
    Task<History> GetHistoryAsync(string ticker, DateTime startDate, DateTime endDate, CancellationToken ct = default);
}

