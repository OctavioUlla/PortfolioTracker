using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortfolioTracker.Data;
using PortfolioTracker.Models;

namespace PortfolioTracker.Controllers;

public class BrokersController : Controller
{
    private readonly AppDbContext _db;

    public BrokersController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var brokers = await _db.Brokers.ToListAsync();
        return View(brokers);
    }

    public IActionResult Create()
    {
        return View(new Broker());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Broker broker)
    {
        if (ModelState.IsValid)
        {
            _db.Brokers.Add(broker);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(broker);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var broker = await _db.Brokers.FindAsync(id);
        if (broker == null) return NotFound();
        return View(broker);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Broker broker)
    {
        if (id != broker.Id) return NotFound();
        if (ModelState.IsValid)
        {
            _db.Update(broker);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(broker);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var broker = await _db.Brokers.FindAsync(id);
        if (broker == null) return NotFound();
        return View(broker);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var broker = await _db.Brokers.FindAsync(id);
        if (broker != null) _db.Brokers.Remove(broker);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
