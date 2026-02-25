using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortfolioTracker.Data;
using PortfolioTracker.Models;
using PortfolioTracker.Services;

namespace PortfolioTracker.Controllers;

public class DashboardController : Controller
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var monthlyBalances = await _db.MonthlyBalances
            .Include(m => m.Broker)
            .OrderBy(m => m.Year).ThenBy(m => m.Month)
            .ToListAsync();

        var cashTransactions = await _db.CashTransactions
            .OrderBy(t => t.Date)
            .ToListAsync();

        var stockTrades = await _db.StockTrades
            .OrderBy(t => t.Date)
            .ToListAsync();

        var viewModel = new DashboardViewModel
        {
            MonthlyBalances = monthlyBalances,
            CashTransactions = cashTransactions,
            StockHoldings = StockHoldingsCalculator.Calculate(stockTrades),
            TotalCurrentBalance = monthlyBalances
                .GroupBy(m => new { m.Year, m.Month })
                .OrderByDescending(g => g.Key.Year).ThenByDescending(g => g.Key.Month)
                .FirstOrDefault()
                ?.Sum(m => m.Balance) ?? 0,
            LifetimeIRR = IrrCalculator.Calculate(cashTransactions, monthlyBalances),
            SP500VirtualPortfolio = SP500Calculator.Calculate(cashTransactions),
        };

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> CalculateIRR(DateTime? startDate, DateTime? endDate, int? year)
    {
        var cashTransactions = await _db.CashTransactions
            .OrderBy(t => t.Date)
            .ToListAsync();
        var monthlyBalances = await _db.MonthlyBalances
            .OrderBy(m => m.Year).ThenBy(m => m.Month)
            .ToListAsync();

        decimal irr;
        string period;

        if (year.HasValue)
        {
            var yearTransactions = cashTransactions.Where(t => t.Date.Year == year.Value).ToList();
            var yearEndBalances = monthlyBalances.Where(m => m.Year == year.Value).ToList();
            var prevYearBalance = monthlyBalances
                .Where(m => m.Year < year.Value)
                .GroupBy(b => new { b.Year, b.Month })
                .OrderByDescending(g => g.Key.Year).ThenByDescending(g => g.Key.Month)
                .FirstOrDefault()
                ?.Sum(b => b.Balance) ?? 0;
            irr = IrrCalculator.Calculate(yearTransactions, yearEndBalances,
                prevYearBalance, new DateTime(year.Value, 1, 1));
            period = year.Value.ToString();
        }
        else if (startDate.HasValue && endDate.HasValue)
        {
            var periodStart = new DateTime(startDate.Value.Year, startDate.Value.Month, 1);
            var rangeTransactions = cashTransactions
                .Where(t => t.Date >= startDate.Value && t.Date <= endDate.Value).ToList();
            var rangeEndBalances = monthlyBalances
                .Where(m => m.Date >= periodStart && m.Date <= endDate.Value).ToList();
            var prevPeriodBalance = monthlyBalances
                .Where(m => m.Date < periodStart)
                .GroupBy(b => new { b.Year, b.Month })
                .OrderByDescending(g => g.Key.Year).ThenByDescending(g => g.Key.Month)
                .FirstOrDefault()
                ?.Sum(b => b.Balance) ?? 0;
            irr = IrrCalculator.Calculate(rangeTransactions, rangeEndBalances,
                prevPeriodBalance, startDate.Value);
            period = $"{startDate.Value:MMM yyyy} - {endDate.Value:MMM yyyy}";
        }
        else
        {
            irr = IrrCalculator.Calculate(cashTransactions, monthlyBalances);
            period = "Lifetime";
        }

        return Json(new { irr, period });
    }
}
