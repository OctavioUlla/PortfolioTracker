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
    /// Everything reported — units, invested, value, IRR — is measured as of the date of the
    /// price used to value it, so transactions made after that date are excluded and
    /// <see cref="SP500VirtualPortfolio.CurrentValue"/> always equals the last
    /// <see cref="SP500VirtualPortfolio.History"/> point the dashboard chart plots.
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
        decimal invested = 0;
        decimal lastTransactionPrice = 0;

        // Units and net invested after each transaction, so the benchmark can be re-valued at
        // any date rather than only at the last one.
        var positionOverTime = new List<(DateTime Date, decimal Units, decimal Invested)>();

        foreach (var t in ordered)
        {
            if (t.SP500Price > 0) lastTransactionPrice = t.SP500Price;

            if (t.Type == TransactionType.Deposit && t.SP500Price > 0)
            {
                units += t.Amount / t.SP500Price;
                invested += t.Amount;
                positionOverTime.Add((t.Date, units, invested));
            }
            else if (t.Type == TransactionType.Withdrawal && units > 0 && t.SP500Price > 0)
            {
                units -= Math.Min(units, t.Amount / t.SP500Price);
                invested -= t.Amount;
                positionOverTime.Add((t.Date, units, invested));
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
            var (unitsThen, _) = PositionAt(positionOverTime, monthEnd);
            // Skip months before the first deposit: DashboardViewModel.SP500ChartData reads a
            // missing entry as a gap in the chart, and a zero-valued point would flatten the
            // line to 0 instead.
            if (unitsThen > 0)
                portfolio.History.Add((monthEnd, unitsThen * price));
        }

        // Pick the price and the date it belongs to together: valuing the units held today at
        // an earlier month's price would put the card above the chart's last point (issue #60).
        // Prefer a price for the portfolio's own end month; fall back to the latest earlier
        // month, then to the last transaction price (the behaviour before month-end prices
        // were recorded).
        decimal terminalPrice;
        DateTime? valuationDate;
        if (pricedMonths.Count > 0)
        {
            var chosen = endMonthEnd.HasValue
                ? pricedMonths.LastOrDefault(p => p.MonthEnd == endMonthEnd.Value)
                : default;
            if (chosen == default) chosen = pricedMonths[^1];

            terminalPrice = chosen.Price;
            valuationDate = chosen.MonthEnd;
        }
        else
        {
            terminalPrice = lastTransactionPrice;
            valuationDate = ordered.LastOrDefault()?.Date;
        }

        portfolio.HasMonthEndPrice = endMonthEnd.HasValue && valuationDate == endMonthEnd.Value;

        var (unitsAtValuation, investedAtValuation) = valuationDate.HasValue
            ? PositionAt(positionOverTime, valuationDate.Value)
            : (0m, 0m);

        portfolio.TotalInvested = Math.Max(0, investedAtValuation);
        portfolio.CurrentUnits = unitsAtValuation;
        portfolio.CurrentValue = unitsAtValuation * terminalPrice;
        portfolio.TotalReturn = portfolio.CurrentValue - portfolio.TotalInvested;
        portfolio.TotalReturnPercent = portfolio.TotalInvested > 0
            ? (portfolio.TotalReturn / portfolio.TotalInvested) * 100
            : 0;

        // Same cash flows as the real portfolio's IRR, ending on the same date — that
        // alignment is the whole point of recording a price per month. CalculateWithEndValue
        // drops the flows dated after the end date for us.
        portfolio.Irr = valuationDate.HasValue
            ? IrrCalculator.CalculateWithEndValue(transactions, portfolio.CurrentValue, valuationDate.Value)
            : 0;

        return portfolio;
    }

    private static DateTime LastDayOfMonth(int year, int month) =>
        new DateTime(year, month, DateTime.DaysInMonth(year, month));

    private static (decimal Units, decimal Invested) PositionAt(
        List<(DateTime Date, decimal Units, decimal Invested)> positionOverTime, DateTime asOf)
    {
        decimal units = 0;
        decimal invested = 0;
        foreach (var point in positionOverTime)
        {
            if (point.Date > asOf) break;
            units = point.Units;
            invested = point.Invested;
        }
        return (units, invested);
    }
}
