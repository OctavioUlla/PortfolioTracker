namespace PortfolioTracker.Models;

public class Goal
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TargetValue { get; set; }
    public DateTime TargetDate { get; set; }
}
