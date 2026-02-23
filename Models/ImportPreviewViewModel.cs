namespace PortfolioTracker.Models;

public class ImportPreviewViewModel
{
    public int BrokerId { get; set; }
    public List<CashTransaction> CashTransactions { get; set; } = new();
    public List<StockTrade> StockTrades { get; set; } = new();
    public List<MonthlyBalance> MonthlyBalances { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
