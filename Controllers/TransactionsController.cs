using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PortfolioTracker.Data;
using PortfolioTracker.Models;

namespace PortfolioTracker.Controllers;

public class TransactionsController : Controller
{
    private readonly AppDbContext _db;

    public TransactionsController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var transactions = await _db.CashTransactions
            .Include(t => t.Broker)
            .OrderByDescending(t => t.Date)
            .ToListAsync();
        return View(transactions);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Brokers = new SelectList(await _db.Brokers.ToListAsync(), "Id", "Name");
        return View(new CashTransaction { Date = DateTime.Today, SP500Price = 0 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CashTransaction transaction)
    {
        if (ModelState.IsValid)
        {
            transaction.Amount = Math.Abs(transaction.Amount);
            _db.CashTransactions.Add(transaction);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Brokers = new SelectList(await _db.Brokers.ToListAsync(), "Id", "Name");
        return View(transaction);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var transaction = await _db.CashTransactions.FindAsync(id);
        if (transaction == null) return NotFound();
        ViewBag.Brokers = new SelectList(await _db.Brokers.ToListAsync(), "Id", "Name");
        return View(transaction);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CashTransaction transaction)
    {
        if (id != transaction.Id) return NotFound();
        if (ModelState.IsValid)
        {
            transaction.Amount = Math.Abs(transaction.Amount);
            _db.Update(transaction);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Brokers = new SelectList(await _db.Brokers.ToListAsync(), "Id", "Name");
        return View(transaction);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var transaction = await _db.CashTransactions
            .Include(t => t.Broker)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (transaction == null) return NotFound();
        return View(transaction);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var transaction = await _db.CashTransactions.FindAsync(id);
        if (transaction != null) _db.CashTransactions.Remove(transaction);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
