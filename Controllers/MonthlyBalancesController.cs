using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PortfolioTracker.Data;
using PortfolioTracker.Models;

namespace PortfolioTracker.Controllers;

public class MonthlyBalancesController : Controller
{
    private readonly AppDbContext _db;

    public MonthlyBalancesController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var balances = await _db.MonthlyBalances
            .Include(b => b.Broker)
            .OrderByDescending(b => b.Year).ThenByDescending(b => b.Month)
            .ToListAsync();
        ViewBag.SP500Prices = await _db.SP500MonthlyPrices.ToDictionaryAsync(p => (p.Year, p.Month), p => p.Price);
        return View(balances);
    }

    public async Task<IActionResult> Create()
    {
        var today = DateTime.Today;
        await PopulateBrokersAsync();
        return View(new MonthlyBalanceFormViewModel
        {
            Year = today.Year,
            Month = today.Month,
            SP500Price = await GetSP500PriceAsync(today.Year, today.Month)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MonthlyBalanceFormViewModel form)
    {
        if (ModelState.IsValid)
        {
            var monthlyBalance = new MonthlyBalance();
            form.ApplyTo(monthlyBalance);
            _db.MonthlyBalances.Add(monthlyBalance);
            await UpsertSP500PriceAsync(form.Year, form.Month, form.SP500Price!.Value);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        await PopulateBrokersAsync();
        return View(form);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var balance = await _db.MonthlyBalances.FindAsync(id);
        if (balance == null) return NotFound();
        await PopulateBrokersAsync();
        return View(MonthlyBalanceFormViewModel.FromEntity(
            balance, await GetSP500PriceAsync(balance.Year, balance.Month)));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, MonthlyBalanceFormViewModel form)
    {
        if (id != form.Id) return NotFound();
        if (ModelState.IsValid)
        {
            var balance = await _db.MonthlyBalances.FindAsync(id);
            if (balance == null) return NotFound();
            form.ApplyTo(balance);
            // The price belongs to the month the balance now sits in; a month the balance
            // moved away from keeps its own price, which other brokers may still rely on.
            await UpsertSP500PriceAsync(form.Year, form.Month, form.SP500Price!.Value);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        await PopulateBrokersAsync();
        return View(form);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var balance = await _db.MonthlyBalances
            .Include(b => b.Broker)
            .FirstOrDefaultAsync(b => b.Id == id);
        if (balance == null) return NotFound();
        ViewBag.SP500Price = await GetSP500PriceAsync(balance.Year, balance.Month);
        return View(balance);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var balance = await _db.MonthlyBalances.FindAsync(id);
        // The month's S&P 500 price is deliberately left in place: it is shared by every
        // broker's balance for that month, and by the historical benchmark series.
        if (balance != null) _db.MonthlyBalances.Remove(balance);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateBrokersAsync()
    {
        ViewBag.Brokers = new SelectList(await _db.Brokers.ToListAsync(), "Id", "Name");
    }

    private async Task<decimal?> GetSP500PriceAsync(int year, int month)
    {
        var existing = await _db.SP500MonthlyPrices
            .FirstOrDefaultAsync(p => p.Year == year && p.Month == month);
        return existing?.Price;
    }

    private async Task UpsertSP500PriceAsync(int year, int month, decimal price)
    {
        var existing = await _db.SP500MonthlyPrices
            .FirstOrDefaultAsync(p => p.Year == year && p.Month == month);
        if (existing == null)
            _db.SP500MonthlyPrices.Add(new SP500MonthlyPrice { Year = year, Month = month, Price = price });
        else
            existing.Price = price;
    }
}
