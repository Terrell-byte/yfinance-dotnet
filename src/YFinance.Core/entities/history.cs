namespace YFinance.Core.Entities;

public class HistoryPoint
{
    public DateTime? date { get; set; }
    public decimal? open { get; set; }
    public decimal? high { get; set; }
    public decimal? low { get; set; }
    public decimal? close { get; set; }
    public decimal? adjClose { get; set; }
    public long? volume { get; set; }
}


public class History
{
    public string? ticker { get; set; }
    public List<HistoryPoint> points { get; set; } = new();
}