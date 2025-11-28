namespace YFinance.Core.Entities;

public class Quote
{
    public string? ticker { get; set; }
    public decimal? ask { get; set; }
    public decimal? bid { get; set; }
    public decimal? open { get; set; }
    public decimal? percentageChange { get; set; }
}