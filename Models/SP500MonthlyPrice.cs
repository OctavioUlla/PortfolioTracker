namespace PortfolioTracker.Models;

/// <summary>
/// Closing S&amp;P 500 price for a calendar month. One row per (Year, Month) — the price is
/// market data, not broker-specific, so it is shared by every broker's monthly balance.
/// </summary>
public class SP500MonthlyPrice
{
    public int Id { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Price { get; set; }

    public DateTime Date => new DateTime(Year, Month, 1);
    public string MonthName => Date.ToString("MMMM yyyy");
}
