namespace YFinance.Yahoo.Interfaces;

public interface IYahooClient
{
    HttpClient GetClient();
    Task<string> GetCrumbAsync(CancellationToken ct = default);
}

