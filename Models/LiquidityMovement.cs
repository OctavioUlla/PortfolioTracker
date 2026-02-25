namespace PortfolioTracker.Models;

public class LiquidityMovement
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
    public int LiquidityAccountId { get; set; }
    public LiquidityAccount? LiquidityAccount { get; set; }
}
