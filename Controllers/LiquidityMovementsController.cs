using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PortfolioTracker.Data;
using PortfolioTracker.Models;

namespace PortfolioTracker.Controllers;

public class LiquidityMovementsController : Controller
{
    private readonly AppDbContext _db;

    public LiquidityMovementsController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(int? accountId)
    {
        var query = _db.LiquidityMovements
            .Include(m => m.LiquidityAccount)
            .AsQueryable();

        if (accountId.HasValue)
            query = query.Where(m => m.LiquidityAccountId == accountId.Value);

        var movements = await query.OrderByDescending(m => m.Date).ToListAsync();

        ViewBag.Accounts = await _db.LiquidityAccounts.ToListAsync();
        ViewBag.SelectedAccountId = accountId;
        return View(movements);
    }

    public async Task<IActionResult> Create(int? accountId)
    {
        ViewBag.Accounts = new SelectList(await _db.LiquidityAccounts.ToListAsync(), "Id", "Name");
        return View(new LiquidityMovement { Date = DateTime.Today, LiquidityAccountId = accountId ?? 0 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LiquidityMovement movement)
    {
        if (ModelState.IsValid)
        {
            _db.LiquidityMovements.Add(movement);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Accounts = new SelectList(await _db.LiquidityAccounts.ToListAsync(), "Id", "Name");
        return View(movement);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var movement = await _db.LiquidityMovements.FindAsync(id);
        if (movement == null) return NotFound();
        ViewBag.Accounts = new SelectList(await _db.LiquidityAccounts.ToListAsync(), "Id", "Name");
        return View(movement);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, LiquidityMovement movement)
    {
        if (id != movement.Id) return NotFound();
        if (ModelState.IsValid)
        {
            _db.Update(movement);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Accounts = new SelectList(await _db.LiquidityAccounts.ToListAsync(), "Id", "Name");
        return View(movement);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var movement = await _db.LiquidityMovements
            .Include(m => m.LiquidityAccount)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (movement == null) return NotFound();
        return View(movement);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var movement = await _db.LiquidityMovements.FindAsync(id);
        if (movement != null) _db.LiquidityMovements.Remove(movement);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
