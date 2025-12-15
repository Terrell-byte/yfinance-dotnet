namespace YFinance.Yahoo.Entities;

public class Sector
{
    public string? name { get; set; }
    public int? companyCount { get; set; }
    public decimal? dailyReturn { get; set; }
    public decimal? annualReturn { get; set; }
}