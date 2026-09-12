using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortfolioTracker.Data;
using PortfolioTracker.Models;

namespace PortfolioTracker.Controllers;

public class StockWatchListController : Controller
{
    private readonly AppDbContext _db;

    public StockWatchListController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        return View(await BuildViewModelAsync(new StockWatchListItemFormViewModel()));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "Form")] StockWatchListItemFormViewModel form)
    {
        if (ModelState.IsValid)
        {
            // The ticker is unique, so check before inserting: a duplicate is a normal
            // mistake to make, and should come back as a validation message rather than
            // as an index violation from SQLite.
            var ticker = StockWatchListItemFormViewModel.Normalize(form.Ticker);
            if (await _db.StockWatchListItems.AnyAsync(w => w.Ticker == ticker))
            {
                ModelState.AddModelError($"Form.{nameof(form.Ticker)}", $"{ticker} is already on your watch list.");
            }
            else
            {
                var item = new StockWatchListItem { AddedDate = DateTime.Today };
                form.ApplyTo(item);
                _db.StockWatchListItems.Add(item);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
        }

        return View(nameof(Index), await BuildViewModelAsync(form));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTarget(int id, decimal targetPrice)
    {
        var item = await _db.StockWatchListItems.FindAsync(id);
        if (item != null && targetPrice > 0)
        {
            item.TargetPrice = targetPrice;
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.StockWatchListItems.FindAsync(id);
        if (item != null)
        {
            _db.StockWatchListItems.Remove(item);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task<StockWatchListViewModel> BuildViewModelAsync(StockWatchListItemFormViewModel form) => new()
    {
        Items = await _db.StockWatchListItems.OrderBy(w => w.Ticker).ToListAsync(),
        Form = form
    };
}
