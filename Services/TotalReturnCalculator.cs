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

        var endValue = latestMonth.Sum(b => b.Balance);
        var netDeposits = transactions.Sum(t =>
            t.Type == TransactionType.Deposit ? t.Amount : -t.Amount);
        var invested = startingBalance + netDeposits;

        return (endValue, invested);
    }
}
