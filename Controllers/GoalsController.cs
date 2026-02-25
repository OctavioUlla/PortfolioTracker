using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortfolioTracker.Data;
using PortfolioTracker.Models;
using PortfolioTracker.Services;

namespace PortfolioTracker.Controllers;

public class GoalsController : Controller
{
    private readonly AppDbContext _db;

    public GoalsController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var goals = await _db.Goals.OrderBy(g => g.TargetDate).ToListAsync();
        var cashTransactions = await _db.CashTransactions.OrderBy(t => t.Date).ToListAsync();
        var monthlyBalances = await _db.MonthlyBalances
            .OrderBy(m => m.Year).ThenBy(m => m.Month)
            .ToListAsync();

        var currentBalance = monthlyBalances
            .GroupBy(m => new { m.Year, m.Month })
            .OrderByDescending(g => g.Key.Year).ThenByDescending(g => g.Key.Month)
            .FirstOrDefault()
            ?.Sum(m => m.Balance) ?? 0;

        var lifetimeIRR = IrrCalculator.Calculate(cashTransactions, monthlyBalances);
        var avgMonthlyDeposit = CalculateAverageMonthlyDeposit(cashTransactions);

        // Build historical data points
        var historicalMonths = monthlyBalances
            .GroupBy(m => new { m.Year, m.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .ToList();

        var histLabels = historicalMonths
            .Select(g => $"\"{g.Key.Year}-{g.Key.Month:D2}\"");
        var histData = historicalMonths
            .Select(g => g.Sum(m => m.Balance).ToString("F2", CultureInfo.InvariantCulture));

        // Build projection data points (default: 10 years forward)
        var projectionMonths = 120;
        var monthlyRate = lifetimeIRR > 0
            ? (decimal)(Math.Pow(1 + (double)lifetimeIRR / 100, 1.0 / 12) - 1)
            : 0m;
        var projLabels = new List<string>();
        var projData = new List<string>();
        var projValue = currentBalance;
        var now = DateTime.Today;

        for (int i = 1; i <= projectionMonths; i++)
        {
            var date = now.AddMonths(i);
            projValue = projValue * (1 + monthlyRate) + avgMonthlyDeposit;
            projLabels.Add($"\"{date:yyyy-MM}\"");
            projData.Add(projValue.ToString("F2", CultureInfo.InvariantCulture));
        }

        var viewModel = new GoalsViewModel
        {
            Goals = goals,
            CurrentPortfolioValue = currentBalance,
            LifetimeIRR = lifetimeIRR,
            AverageMonthlyDeposit = avgMonthlyDeposit,
            HistoricalChartLabels = string.Join(",", histLabels),
            HistoricalChartData = string.Join(",", histData),
            ProjectionChartLabels = string.Join(",", projLabels),
            ProjectionChartData = string.Join(",", projData),
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Goal goal)
    {
        if (ModelState.IsValid)
        {
            _db.Goals.Add(goal);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var goal = await _db.Goals.FindAsync(id);
        if (goal != null)
        {
            _db.Goals.Remove(goal);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Extrapolate(decimal monthlyDeposit, decimal annualIRR)
    {
        var monthlyBalances = await _db.MonthlyBalances
            .OrderBy(m => m.Year).ThenBy(m => m.Month)
            .ToListAsync();

        var goals = await _db.Goals.OrderBy(g => g.TargetDate).ToListAsync();

        var currentBalance = monthlyBalances
            .GroupBy(m => new { m.Year, m.Month })
            .OrderByDescending(g => g.Key.Year).ThenByDescending(g => g.Key.Month)
            .FirstOrDefault()
            ?.Sum(m => m.Balance) ?? 0;

        var monthlyRate = annualIRR != 0
            ? (decimal)(Math.Pow(1 + (double)annualIRR / 100, 1.0 / 12) - 1)
            : 0m;

        // Project 10 years
        var projectionMonths = 120;
        var projLabels = new List<string>();
        var projData = new List<decimal>();
        var projValue = currentBalance;
        var now = DateTime.Today;

        for (int i = 1; i <= projectionMonths; i++)
        {
            var date = now.AddMonths(i);
            projValue = projValue * (1 + monthlyRate) + monthlyDeposit;
            projLabels.Add(date.ToString("yyyy-MM"));
            projData.Add(Math.Round(projValue, 2));
        }

        // Calculate goal statuses
        var goalStatuses = goals.Select(g =>
        {
            var monthsToGoal = ((g.TargetDate.Year - now.Year) * 12) + g.TargetDate.Month - now.Month;
            decimal projectedAtTarget = currentBalance;
            for (int i = 0; i < monthsToGoal && i < projectionMonths; i++)
            {
                projectedAtTarget = projectedAtTarget * (1 + monthlyRate) + monthlyDeposit;
            }

            // Months needed to reach goal value
            int monthsNeeded = 0;
            decimal runningValue = currentBalance;
            while (runningValue < g.TargetValue && monthsNeeded < 600)
            {
                runningValue = runningValue * (1 + monthlyRate) + monthlyDeposit;
                monthsNeeded++;
            }

            return new
            {
                g.Id,
                g.Name,
                TargetValue = g.TargetValue,
                TargetDate = g.TargetDate.ToString("yyyy-MM-dd"),
                ProjectedValue = Math.Round(projectedAtTarget, 2),
                OnTrack = projectedAtTarget >= g.TargetValue,
                MonthsNeeded = monthsNeeded,
                EstimatedDate = now.AddMonths(monthsNeeded).ToString("yyyy-MM")
            };
        }).ToList();

        return Json(new
        {
            labels = projLabels,
            data = projData,
            goals = goalStatuses
        });
    }

    private static decimal CalculateAverageMonthlyDeposit(List<CashTransaction> transactions)
    {
        var deposits = transactions.Where(t => t.Type == TransactionType.Deposit).ToList();
        if (!deposits.Any()) return 0;

        var firstDate = deposits.Min(t => t.Date);
        var lastDate = deposits.Max(t => t.Date);
        var totalMonths = ((lastDate.Year - firstDate.Year) * 12) + lastDate.Month - firstDate.Month;
        if (totalMonths <= 0) totalMonths = 1;

        return Math.Round(deposits.Sum(t => t.Amount) / totalMonths, 2);
    }
}
