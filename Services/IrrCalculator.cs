using PortfolioTracker.Models;

namespace PortfolioTracker.Services;

public static class IrrCalculator
{
    /// <summary>
    /// Calculate IRR using XIRR method (Newton-Raphson), ending at the last day of the
    /// latest month present in <paramref name="balances"/> with that month's total balance.
    /// Returns annualized rate as a percentage (e.g. 12.50 means 12.50%).
    /// </summary>
    /// <param name="startingBalance">
    /// Portfolio value at the start of the period (used for sub-period calculations).
    /// Treated as a negative cash flow (cost) at <paramref name="periodStart"/>.
    /// </param>
    /// <param name="periodStart">Date of the starting balance cash flow.</param>
    public static decimal Calculate(List<CashTransaction> transactions, List<MonthlyBalance> balances,
        decimal startingBalance = 0, DateTime? periodStart = null)
    {
        if (!balances.Any()) return 0;

        var latestMonth = balances
            .GroupBy(b => new { b.Year, b.Month })
            .OrderByDescending(g => g.Key.Year).ThenByDescending(g => g.Key.Month)
            .FirstOrDefault();

        if (latestMonth == null) return 0;

        var endDate = new DateTime(latestMonth.Key.Year, latestMonth.Key.Month,
            DateTime.DaysInMonth(latestMonth.Key.Year, latestMonth.Key.Month));
        var endValue = latestMonth.Sum(b => b.Balance);

        return CalculateWithEndValue(transactions, endValue, endDate, startingBalance, periodStart);
    }

    /// <summary>
    /// Calculate IRR using XIRR method (Newton-Raphson) against an explicit end value and date.
    /// Deposits are negative cash flows; withdrawals and the end value are positive.
    /// Returns annualized rate as a percentage (e.g. 12.50 means 12.50%).
    /// </summary>
    /// <param name="endValue">Portfolio value at <paramref name="endDate"/>, the final positive cash flow.</param>
    /// <param name="endDate">Date the end value is measured at.</param>
    /// <param name="startingBalance">
    /// Portfolio value at the start of the period (used for sub-period calculations).
    /// Treated as a negative cash flow (cost) at <paramref name="periodStart"/>.
    /// </param>
    /// <param name="periodStart">Date of the starting balance cash flow.</param>
    public static decimal CalculateWithEndValue(List<CashTransaction> transactions,
        decimal endValue, DateTime endDate, decimal startingBalance = 0, DateTime? periodStart = null)
    {
        // Newton-Raphson never throws on divergence, it just clamps to -0.999. Without this
        // guard an end value of 0 renders as a plausible-looking "-99.90%" instead of "no data".
        if (endValue <= 0) return 0;

        var cashFlows = new List<(DateTime date, double amount)>();

        if (startingBalance > 0 && periodStart.HasValue)
            cashFlows.Add((periodStart.Value, -(double)startingBalance));

        foreach (var t in transactions.OrderBy(t => t.Date))
        {
            if (t.Type == TransactionType.Deposit)
                cashFlows.Add((t.Date, -(double)t.Amount));
            else
                cashFlows.Add((t.Date, (double)t.Amount));
        }

        cashFlows.Add((endDate, (double)endValue));

        if (cashFlows.Count < 2) return 0;
        // A rate is only meaningful if something was actually paid in.
        if (!cashFlows.Any(cf => cf.amount < 0)) return 0;

        try
        {
            var xirr = CalculateXIRR(cashFlows);
            return (decimal)Math.Round(xirr * 100, 2);
        }
        catch
        {
            return 0;
        }
    }

    private static double CalculateXIRR(List<(DateTime date, double amount)> cashFlows)
    {
        var dates = cashFlows.Select(cf => cf.date).ToList();
        var amounts = cashFlows.Select(cf => cf.amount).ToList();
        var startDate = dates.Min();

        double rate = 0.1;

        for (int iter = 0; iter < 200; iter++)
        {
            double npv = 0;
            double npvDerivative = 0;

            for (int i = 0; i < amounts.Count; i++)
            {
                double t = (dates[i] - startDate).Days / 365.0;
                double factor = Math.Pow(1 + rate, t);
                npv += amounts[i] / factor;
                npvDerivative -= t * amounts[i] / (factor * (1 + rate));
            }

            if (Math.Abs(npv) < 0.001) break;
            if (Math.Abs(npvDerivative) < 1e-10) break;

            rate -= npv / npvDerivative;
            if (rate < -0.999) rate = -0.999;
        }

        return rate;
    }
}
