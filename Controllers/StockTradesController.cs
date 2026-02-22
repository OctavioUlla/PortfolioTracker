using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PortfolioTracker.Data;
using PortfolioTracker.Models;

namespace PortfolioTracker.Controllers;

public class StockTradesController : Controller
{
    private readonly AppDbContext _db;

    public StockTradesController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var trades = await _db.StockTrades
            .Include(t => t.Broker)
            .OrderByDescending(t => t.Date)
            .ToListAsync();
        return View(trades);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Brokers = new SelectList(await _db.Brokers.ToListAsync(), "Id", "Name");
        return View(new StockTrade { Date = DateTime.Today });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StockTrade trade)
    {
        if (ModelState.IsValid)
        {
            _db.StockTrades.Add(trade);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Brokers = new SelectList(await _db.Brokers.ToListAsync(), "Id", "Name");
        return View(trade);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var trade = await _db.StockTrades.FindAsync(id);
        if (trade == null) return NotFound();
        ViewBag.Brokers = new SelectList(await _db.Brokers.ToListAsync(), "Id", "Name");
        return View(trade);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, StockTrade trade)
    {
        if (id != trade.Id) return NotFound();
        if (ModelState.IsValid)
        {
            _db.Update(trade);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Brokers = new SelectList(await _db.Brokers.ToListAsync(), "Id", "Name");
        return View(trade);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var trade = await _db.StockTrades
            .Include(t => t.Broker)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (trade == null) return NotFound();
        return View(trade);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var trade = await _db.StockTrades.FindAsync(id);
        if (trade != null) _db.StockTrades.Remove(trade);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
