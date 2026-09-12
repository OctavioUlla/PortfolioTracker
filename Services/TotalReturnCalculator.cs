using PortfolioTracker.Models;

namespace PortfolioTracker.Services;

public static class TotalReturnCalculator
{
    /// <summary>
    /// Calculate total (absolute) return as a percentage.
    /// Formula: (EndValue - StartValue - NetDeposits) / (StartValue + NetDeposits) * 100
    /// For lifetime: startingBalance = 0.
    /// </summary>
    public static decimal Calculate(List<CashTransaction> transactions, List<MonthlyBalance> balances,
        decimal startingBalance = 0)
    {
        var (endValue, invested) = GetComponents(transactions, balances, startingBalance);
        if (invested == 0) return 0;
        return Math.Round((endValue - invested) / invested * 100, 2);
    }

    /// <summary>
    /// Calculate total (absolute) return as an absolute money amount.
    /// Formula: EndValue - StartValue - NetDeposits
    /// </summary>
    public static decimal CalculateAmount(List<CashTransaction> transactions, List<MonthlyBalance> balances,
        decimal startingBalance = 0)
    {
        var (endValue, invested) = GetComponents(transactions, balances, startingBalance);
        return Math.Round(endValue - invested, 2);
    }

    private static (decimal endValue, decimal invested) GetComponents(
        List<CashTransaction> transactions, List<MonthlyBalance> balances, decimal startingBalance)
    {
        if (!balances.Any()) return (0, 0);

        var latestMonth = balances
            .GroupBy(b => new { b.Year, b.Month })
            .OrderByDescending(g => g.Key.Year).ThenByDescending(g => g.Key.Month)
            .FirstOrDefault();

        if (latestMonth == null) return (0, 0);

        var endDate = new DateTime(latestMonth.Key.Year, latestMonth.Key.Month,
            DateTime.DaysInMonth(latestMonth.Key.Year, latestMonth.Key.Month));

        var endValue = latestMonth.Sum(b => b.Balance);
        // Only what was paid in by the time the end value was measured: a deposit made after
        // that month end is not yet reflected in the balance, so counting it would show a
        // phantom loss of exactly that amount.
        var netDeposits = transactions
            .Where(t => t.Date <= endDate)
            .Sum(t => t.Type == TransactionType.Deposit ? t.Amount : -t.Amount);
        var invested = startingBalance + netDeposits;

        return (endValue, invested);
    }
}
