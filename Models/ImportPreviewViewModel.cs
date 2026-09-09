namespace PortfolioTracker.Models;

public class ImportPreviewViewModel
{
    public int BrokerId { get; set; }
    public List<CashTransaction> CashTransactions { get; set; } = new();
    public List<StockTrade> StockTrades { get; set; } = new();
    public List<MonthlyBalance> MonthlyBalances { get; set; } = new();
    public List<SP500MonthlyPrice> SP500MonthlyPrices { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
