namespace PortfolioTracker.Models;

/// <summary>
/// A stock the portfolio owner is considering buying, together with the price they would
/// want to buy it at. Nothing here is a position — the watch list is a plain shortlist, and
/// the app has no market data feed to compare the target against.
/// </summary>
public class StockWatchListItem
{
    public int Id { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public string? Name { get; set; }
    public decimal TargetPrice { get; set; }
    public string? Notes { get; set; }
    public DateTime AddedDate { get; set; }
}
