using PortfolioTracker.Models;

namespace PortfolioTracker.Services;

public class SP500VirtualPortfolio
{
    public decimal TotalInvested { get; set; }
    public decimal CurrentUnits { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal TotalReturn { get; set; }
    public decimal TotalReturnPercent { get; set; }
    public List<(DateTime Date, decimal Value)> History { get; set; } = new();
}

public static class SP500Calculator
{
    public static SP500VirtualPortfolio Calculate(List<CashTransaction> transactions)
    {
        var portfolio = new SP500VirtualPortfolio();
        decimal units = 0;
        decimal totalInvested = 0;
        decimal lastSP500Price = 0;

        foreach (var t in transactions.OrderBy(t => t.Date))
        {
            if (t.SP500Price > 0) lastSP500Price = t.SP500Price;

            if (t.Type == TransactionType.Deposit && t.SP500Price > 0)
            {
                decimal unitsToAdd = t.Amount / t.SP500Price;
                units += unitsToAdd;
                totalInvested += t.Amount;
                portfolio.History.Add((t.Date, units * t.SP500Price));
            }
            else if (t.Type == TransactionType.Withdrawal && units > 0 && t.SP500Price > 0)
            {
                decimal unitsToSell = Math.Min(units, t.Amount / t.SP500Price);
                units -= unitsToSell;
                totalInvested -= t.Amount;
                portfolio.History.Add((t.Date, units * t.SP500Price));
            }
        }

        portfolio.TotalInvested = Math.Max(0, totalInvested);
        portfolio.CurrentUnits = units;
        portfolio.CurrentValue = units * lastSP500Price;
        portfolio.TotalReturn = portfolio.CurrentValue - portfolio.TotalInvested;
        portfolio.TotalReturnPercent = portfolio.TotalInvested > 0
            ? (portfolio.TotalReturn / portfolio.TotalInvested) * 100
            : 0;

        return portfolio;
    }
}
