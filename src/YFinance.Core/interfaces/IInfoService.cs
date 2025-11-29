using YFinance.Core.Entities;
namespace YFinance.Core.Interfaces;

public interface IInfoService
{
    Task<Info> GetInfoAsync(string ticker, CancellationToken ct = default);
}