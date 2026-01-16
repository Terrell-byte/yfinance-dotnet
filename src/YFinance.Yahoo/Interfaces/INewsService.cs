using YFinance.Yahoo.Entities;

namespace YFinance.Yahoo.Interfaces;

public interface INewsService
{
    Task<IndustryNewsResponse> GetIndustryNewsAsync(
        IEnumerable<IndustryTag> tags,
        int count = 60,
        TimeSpan? window = null,
        CancellationToken ct = default);
}

