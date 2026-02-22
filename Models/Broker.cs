namespace PortfolioTracker.Models;

public class Broker
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<MonthlyBalance> MonthlyBalances { get; set; } = new List<MonthlyBalance>();
    public ICollection<CashTransaction> CashTransactions { get; set; } = new List<CashTransaction>();
    public ICollection<StockTrade> StockTrades { get; set; } = new List<StockTrade>();
}
