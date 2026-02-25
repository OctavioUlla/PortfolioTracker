using PortfolioTracker.Services;

namespace PortfolioTracker.Models;

public class DashboardViewModel
{
    public List<MonthlyBalance> MonthlyBalances { get; set; } = new();
    public List<CashTransaction> CashTransactions { get; set; } = new();
    public List<StockHoldingViewModel> StockHoldings { get; set; } = new();
    public decimal TotalCurrentBalance { get; set; }
    public decimal LifetimeIRR { get; set; }
    public decimal LifetimeTotalReturn { get; set; }
    public SP500VirtualPortfolio SP500VirtualPortfolio { get; set; } = new();

    // For portfolio chart: one data point per month (sum of all brokers)
    public string PortfolioChartLabels => string.Join(",",
        MonthlyBalances
            .GroupBy(m => new { m.Year, m.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => $"\"{g.Key.Year}-{g.Key.Month:D2}\""));

    public string PortfolioChartData => string.Join(",",
        MonthlyBalances
            .GroupBy(m => new { m.Year, m.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => g.Sum(m => m.Balance).ToString("F2", System.Globalization.CultureInfo.InvariantCulture)));

    // S&P 500 chart data aligned to same months as portfolio
    public string SP500ChartData
    {
        get
        {
            var months = MonthlyBalances
                .GroupBy(m => new { m.Year, m.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new DateTime(g.Key.Year, g.Key.Month, 1))
                .ToList();

            if (!months.Any() || !SP500VirtualPortfolio.History.Any())
                return string.Empty;

            var result = new List<string>();
            foreach (var month in months)
            {
                // Find the last S&P 500 history entry on or before this month's end
                var monthEnd = new DateTime(month.Year, month.Month, DateTime.DaysInMonth(month.Year, month.Month));
                var entry = SP500VirtualPortfolio.History
                    .Where(h => h.Date <= monthEnd)
                    .OrderByDescending(h => h.Date)
                    .FirstOrDefault();
                result.Add(entry == default
                    ? "null"
                    : entry.Value.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
            }
            return string.Join(",", result);
        }
    }
}
