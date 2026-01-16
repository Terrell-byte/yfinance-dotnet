using System.Collections.Generic;

namespace YFinance.Yahoo.Entities;

public class IndustryNewsResponse
{
    public Dictionary<string, Dictionary<string, List<Article>>> sectors { get; init; } =
        new(StringComparer.OrdinalIgnoreCase);
}

