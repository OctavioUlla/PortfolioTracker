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

        var viewModel = new DashboardViewModel
        {
            MonthlyBalances = monthlyBalances,
            CashTransactions = cashTransactions,
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
            irr = IrrCalculator.Calculate(yearTransactions, yearEndBalances);
            period = year.Value.ToString();
        }
        else if (startDate.HasValue && endDate.HasValue)
        {
            var rangeTransactions = cashTransactions
                .Where(t => t.Date >= startDate.Value && t.Date <= endDate.Value).ToList();
            var rangeEndBalances = monthlyBalances
                .Where(m => m.Date >= startDate.Value && m.Date <= endDate.Value).ToList();
            irr = IrrCalculator.Calculate(rangeTransactions, rangeEndBalances);
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
