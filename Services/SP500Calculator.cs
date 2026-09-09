using PortfolioTracker.Models;

namespace PortfolioTracker.Services;

public class SP500VirtualPortfolio
{
    public decimal TotalInvested { get; set; }
    public decimal CurrentUnits { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal TotalReturn { get; set; }
    public decimal TotalReturnPercent { get; set; }
    public decimal Irr { get; set; }

    /// <summary>
    /// True when the benchmark was valued with a recorded price for the portfolio's own end
    /// month, so its value and IRR are measured at the same date as the real portfolio's.
    /// False means the price is stale (an earlier month's, or the last transaction's).
    /// </summary>
    public bool HasMonthEndPrice { get; set; }

    public List<(DateTime Date, decimal Value)> History { get; set; } = new();
}

public static class SP500Calculator
{
    /// <summary>
    /// Value the simulated S&amp;P 500 portfolio: the same deposits and withdrawals as the
    /// real portfolio, converted to index units at the price recorded on each transaction.
    /// </summary>
    /// <param name="monthlyPrices">Recorded month-end S&amp;P 500 prices, used to value the
    /// benchmark on the same monthly grid the real portfolio is measured on.</param>
    /// <param name="endMonthEnd">
    /// Last day of the real portfolio's latest monthly-balance month — the date the benchmark
    /// is valued at, so its IRR is comparable with the real portfolio's. Passed in rather than
    /// derived from <paramref name="monthlyPrices"/>, which can hold months that have no
    /// balance (a deleted balance, a month the backfill skipped) and would drift the end date.
    /// </param>
    public static SP500VirtualPortfolio Calculate(List<CashTransaction> transactions,
        List<SP500MonthlyPrice> monthlyPrices, DateTime? endMonthEnd)
    {
        var portfolio = new SP500VirtualPortfolio();
        var ordered = transactions.OrderBy(t => t.Date).ToList();

        decimal units = 0;
        decimal totalInvested = 0;
        decimal lastTransactionPrice = 0;

        // Units held after each transaction, so the benchmark can be re-valued at any date.
        var unitsOverTime = new List<(DateTime Date, decimal Units)>();

        foreach (var t in ordered)
        {
            if (t.SP500Price > 0) lastTransactionPrice = t.SP500Price;

            if (t.Type == TransactionType.Deposit && t.SP500Price > 0)
            {
                units += t.Amount / t.SP500Price;
                totalInvested += t.Amount;
                unitsOverTime.Add((t.Date, units));
            }
            else if (t.Type == TransactionType.Withdrawal && units > 0 && t.SP500Price > 0)
            {
                units -= Math.Min(units, t.Amount / t.SP500Price);
                totalInvested -= t.Amount;
                unitsOverTime.Add((t.Date, units));
            }
        }

        var pricedMonths = monthlyPrices
            .Where(p => p.Price > 0)
            .Select(p => (MonthEnd: LastDayOfMonth(p.Year, p.Month), p.Price))
            .Where(p => !endMonthEnd.HasValue || p.MonthEnd <= endMonthEnd.Value)
            .OrderBy(p => p.MonthEnd)
            .ToList();

        foreach (var (monthEnd, price) in pricedMonths)
        {
            var unitsThen = UnitsAt(unitsOverTime, monthEnd);
            // Skip months before the first deposit: DashboardViewModel.SP500ChartData reads a
            // missing entry as a gap in the chart, and a zero-valued point would flatten the
            // line to 0 instead.
            if (unitsThen > 0)
                portfolio.History.Add((monthEnd, unitsThen * price));
        }

        // Prefer a price for the portfolio's own end month; fall back to the latest earlier
        // month, then to the last transaction price (the behaviour before month-end prices
        // were recorded).
        decimal terminalPrice;
        if (endMonthEnd.HasValue)
        {
            var exact = pricedMonths.LastOrDefault(p => p.MonthEnd == endMonthEnd.Value);
            portfolio.HasMonthEndPrice = exact != default;
            terminalPrice = exact != default
                ? exact.Price
                : pricedMonths.Count > 0 ? pricedMonths[^1].Price : lastTransactionPrice;
        }
        else
        {
            terminalPrice = pricedMonths.Count > 0 ? pricedMonths[^1].Price : lastTransactionPrice;
        }

        portfolio.TotalInvested = Math.Max(0, totalInvested);
        portfolio.CurrentUnits = units;
        portfolio.CurrentValue = units * terminalPrice;
        portfolio.TotalReturn = portfolio.CurrentValue - portfolio.TotalInvested;
        portfolio.TotalReturnPercent = portfolio.TotalInvested > 0
            ? (portfolio.TotalReturn / portfolio.TotalInvested) * 100
            : 0;

        // Same cash flows as the real portfolio's IRR, ending on the same date — that
        // alignment is the whole point of recording a price per month.
        var irrEndDate = endMonthEnd
            ?? (pricedMonths.Count > 0 ? pricedMonths[^1].MonthEnd : (DateTime?)null)
            ?? ordered.LastOrDefault()?.Date;
        portfolio.Irr = irrEndDate.HasValue
            ? IrrCalculator.CalculateWithEndValue(transactions, portfolio.CurrentValue, irrEndDate.Value)
            : 0;

        return portfolio;
    }

    private static DateTime LastDayOfMonth(int year, int month) =>
        new DateTime(year, month, DateTime.DaysInMonth(year, month));

    private static decimal UnitsAt(List<(DateTime Date, decimal Units)> unitsOverTime, DateTime asOf)
    {
        decimal units = 0;
        foreach (var point in unitsOverTime)
        {
            if (point.Date > asOf) break;
            units = point.Units;
        }
        return units;
    }
}
