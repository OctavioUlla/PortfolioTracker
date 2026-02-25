namespace PortfolioTracker.Models;

public class LiquidityAccount
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public ICollection<LiquidityMovement> Movements { get; set; } = new List<LiquidityMovement>();
}
