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
        return View(balances);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Brokers = new SelectList(await _db.Brokers.ToListAsync(), "Id", "Name");
        return View(new MonthlyBalance { Year = DateTime.Today.Year, Month = DateTime.Today.Month });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MonthlyBalance monthlyBalance)
    {
        if (ModelState.IsValid)
        {
            _db.MonthlyBalances.Add(monthlyBalance);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Brokers = new SelectList(await _db.Brokers.ToListAsync(), "Id", "Name");
        return View(monthlyBalance);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var balance = await _db.MonthlyBalances.FindAsync(id);
        if (balance == null) return NotFound();
        ViewBag.Brokers = new SelectList(await _db.Brokers.ToListAsync(), "Id", "Name");
        return View(balance);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, MonthlyBalance monthlyBalance)
    {
        if (id != monthlyBalance.Id) return NotFound();
        if (ModelState.IsValid)
        {
            _db.Update(monthlyBalance);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Brokers = new SelectList(await _db.Brokers.ToListAsync(), "Id", "Name");
        return View(monthlyBalance);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var balance = await _db.MonthlyBalances
            .Include(b => b.Broker)
            .FirstOrDefaultAsync(b => b.Id == id);
        if (balance == null) return NotFound();
        return View(balance);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var balance = await _db.MonthlyBalances.FindAsync(id);
        if (balance != null) _db.MonthlyBalances.Remove(balance);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
