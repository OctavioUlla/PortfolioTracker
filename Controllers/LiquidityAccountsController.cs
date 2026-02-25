using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortfolioTracker.Data;
using PortfolioTracker.Models;

namespace PortfolioTracker.Controllers;

public class LiquidityAccountsController : Controller
{
    private readonly AppDbContext _db;

    public LiquidityAccountsController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var accounts = await _db.LiquidityAccounts
            .Include(a => a.Movements)
            .ToListAsync();
        return View(accounts);
    }

    public IActionResult Create()
    {
        return View(new LiquidityAccount());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LiquidityAccount account)
    {
        if (ModelState.IsValid)
        {
            _db.LiquidityAccounts.Add(account);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(account);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var account = await _db.LiquidityAccounts.FindAsync(id);
        if (account == null) return NotFound();
        return View(account);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, LiquidityAccount account)
    {
        if (id != account.Id) return NotFound();
        if (ModelState.IsValid)
        {
            _db.Update(account);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(account);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var account = await _db.LiquidityAccounts
            .Include(a => a.Movements)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (account == null) return NotFound();
        return View(account);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var account = await _db.LiquidityAccounts.FindAsync(id);
        if (account != null) _db.LiquidityAccounts.Remove(account);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
