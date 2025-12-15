namespace YFinance.Yahoo.Interfaces;
using YFinance.Yahoo.Entities;
public interface ISectorService
{
    Task<Sector[]> GetSectorsAsync(string[] sectors, CancellationToken ct = default);
}
