namespace PortfolioTracker.Models;

public enum TradeType { Buy, Sell }

public class StockTrade
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public TradeType Type { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Commission { get; set; }
    public string? Notes { get; set; }
    public int BrokerId { get; set; }
    public Broker Broker { get; set; } = null!;

    public decimal TotalValue => Quantity * Price;
}
