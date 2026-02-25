namespace PortfolioTracker.Models;

public class GoalsViewModel
{
    public List<Goal> Goals { get; set; } = new();
    public decimal CurrentPortfolioValue { get; set; }
    public decimal LifetimeIRR { get; set; }
    public decimal AverageMonthlyDeposit { get; set; }

    /// <summary>
    /// Projected portfolio values per month (JSON array of {label, value}).
    /// Generated server-side using default IRR and monthly deposit.
    /// </summary>
    public string ProjectionChartLabels { get; set; } = "[]";
    public string ProjectionChartData { get; set; } = "[]";
    public string HistoricalChartLabels { get; set; } = "[]";
    public string HistoricalChartData { get; set; } = "[]";
}
