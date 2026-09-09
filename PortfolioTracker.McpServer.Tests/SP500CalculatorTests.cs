using PortfolioTracker.Models;
using PortfolioTracker.Services;

namespace PortfolioTracker.McpServer.Tests;

[TestFixture]
public class SP500CalculatorTests
{
    private static CashTransaction Deposit(string date, decimal amount, decimal sp500Price) =>
        new()
        {
            Date = DateTime.Parse(date),
            Type = TransactionType.Deposit,
            Amount = amount,
            SP500Price = sp500Price,
            BrokerId = 1
        };

    private static SP500MonthlyPrice Price(int year, int month, decimal price) =>
        new() { Year = year, Month = month, Price = price };

    private static MonthlyBalance Balance(int year, int month, decimal balance) =>
        new() { Year = year, Month = month, Balance = balance, BrokerId = 1 };

    [Test]
    public void Calculate_UsesRecordedMonthEndPrice_NotLastTransactionPrice()
    {
        // One deposit in January, then the index rises through June with no further deposits.
        var transactions = new List<CashTransaction> { Deposit("2024-01-31", 10000, 5000) };
        var prices = new List<SP500MonthlyPrice> { Price(2024, 1, 5000), Price(2024, 6, 6000) };

        var portfolio = SP500Calculator.Calculate(transactions, prices, new DateTime(2024, 6, 30));

        // 2 units at 6000, not the stale 5000 from the January transaction.
        Assert.That(portfolio.CurrentUnits, Is.EqualTo(2));
        Assert.That(portfolio.CurrentValue, Is.EqualTo(12000));
        Assert.That(portfolio.TotalReturnPercent, Is.EqualTo(20));
        Assert.That(portfolio.HasMonthEndPrice, Is.True);
    }

    [Test]
    public void Calculate_VirtualIrr_EndsOnSameDateAsRealPortfolioIrr()
    {
        var transactions = new List<CashTransaction> { Deposit("2024-01-31", 10000, 5000) };
        var prices = new List<SP500MonthlyPrice> { Price(2024, 1, 5000), Price(2024, 12, 6000) };
        var endMonthEnd = new DateTime(2024, 12, 31);

        var portfolio = SP500Calculator.Calculate(transactions, prices, endMonthEnd);

        // The real portfolio ending at the same value on the same date must produce the
        // same IRR — that equivalence is the point of recording a price per month.
        var balances = new List<MonthlyBalance> { Balance(2024, 12, portfolio.CurrentValue) };
        var realIrr = IrrCalculator.Calculate(transactions, balances);

        Assert.That(portfolio.Irr, Is.EqualTo(realIrr));
        Assert.That(portfolio.Irr, Is.GreaterThan(0));
    }

    [Test]
    public void Calculate_NoPriceForEndMonth_FallsBackToLatestEarlierMonthAndFlagsIt()
    {
        var transactions = new List<CashTransaction> { Deposit("2024-01-31", 10000, 5000) };
        var prices = new List<SP500MonthlyPrice> { Price(2024, 1, 5000), Price(2024, 5, 5500) };

        var portfolio = SP500Calculator.Calculate(transactions, prices, new DateTime(2024, 6, 30));

        Assert.That(portfolio.CurrentValue, Is.EqualTo(11000));
        Assert.That(portfolio.HasMonthEndPrice, Is.False);
    }

    [Test]
    public void Calculate_NoMonthlyPrices_FallsBackToLastTransactionPrice()
    {
        var transactions = new List<CashTransaction>
        {
            Deposit("2024-01-31", 10000, 5000),
            Deposit("2024-03-31", 5000, 5200)
        };

        var portfolio = SP500Calculator.Calculate(transactions, new List<SP500MonthlyPrice>(),
            new DateTime(2024, 6, 30));

        var expectedUnits = 10000m / 5000m + 5000m / 5200m;
        Assert.That(portfolio.CurrentUnits, Is.EqualTo(expectedUnits));
        Assert.That(portfolio.CurrentValue, Is.EqualTo(expectedUnits * 5200m));
        Assert.That(portfolio.HasMonthEndPrice, Is.False);
    }

    [Test]
    public void Calculate_PricesAfterTheEndMonth_AreIgnored()
    {
        var transactions = new List<CashTransaction> { Deposit("2024-01-31", 10000, 5000) };
        var prices = new List<SP500MonthlyPrice>
        {
            Price(2024, 1, 5000),
            Price(2024, 6, 6000),
            Price(2024, 9, 9000)   // later than the portfolio's own end month
        };

        var portfolio = SP500Calculator.Calculate(transactions, prices, new DateTime(2024, 6, 30));

        Assert.That(portfolio.CurrentValue, Is.EqualTo(12000));
        Assert.That(portfolio.History.Select(h => h.Date),
            Is.EqualTo(new[] { new DateTime(2024, 1, 31), new DateTime(2024, 6, 30) }));
    }

    [Test]
    public void Calculate_MonthsBeforeFirstDeposit_ProduceNoHistoryPoint()
    {
        // Otherwise a zero-valued point would flatten the dashboard chart's benchmark line
        // to 0 instead of leaving a gap before the first deposit.
        var transactions = new List<CashTransaction> { Deposit("2024-03-31", 10000, 5000) };
        var prices = new List<SP500MonthlyPrice>
        {
            Price(2024, 1, 4800),
            Price(2024, 2, 4900),
            Price(2024, 3, 5000)
        };

        var portfolio = SP500Calculator.Calculate(transactions, prices, new DateTime(2024, 3, 31));

        Assert.That(portfolio.History, Has.Count.EqualTo(1));
        Assert.That(portfolio.History[0].Date, Is.EqualTo(new DateTime(2024, 3, 31)));
    }

    [Test]
    public void Calculate_NoTransactions_ReturnsZeroesRatherThanAFabricatedRate()
    {
        var portfolio = SP500Calculator.Calculate(new List<CashTransaction>(),
            new List<SP500MonthlyPrice> { Price(2024, 6, 6000) }, new DateTime(2024, 6, 30));

        Assert.That(portfolio.CurrentValue, Is.EqualTo(0));
        Assert.That(portfolio.Irr, Is.EqualTo(0));
    }

    [Test]
    public void CalculateWithEndValue_ZeroEndValue_ReturnsZero()
    {
        // Newton-Raphson clamps to -0.999 rather than throwing, so without an explicit
        // guard a missing end value renders as a plausible-looking "-99.90%".
        var transactions = new List<CashTransaction> { Deposit("2024-01-31", 10000, 5000) };

        var irr = IrrCalculator.CalculateWithEndValue(transactions, 0, new DateTime(2024, 12, 31));

        Assert.That(irr, Is.EqualTo(0));
    }
}
