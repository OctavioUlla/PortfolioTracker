using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PortfolioTracker.Data;
using PortfolioTracker.Models;
using PortfolioTracker.Services;

namespace PortfolioTracker.Controllers;

public class ImportController : Controller
{
    private readonly AppDbContext _db;
    private readonly ExcelImportService _importService;
    private const string SessionKey = "ImportPreview";

    public ImportController(AppDbContext db, ExcelImportService importService)
    {
        _db = db;
        _importService = importService;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.Brokers = new SelectList(await _db.Brokers.ToListAsync(), "Id", "Name");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile file, int brokerId)
    {
        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Please select an Excel file to upload.");
            ViewBag.Brokers = new SelectList(await _db.Brokers.ToListAsync(), "Id", "Name");
            return View("Index");
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".xlsx" && ext != ".xls")
        {
            ModelState.AddModelError(string.Empty, "Only Excel files (.xlsx, .xls) are supported.");
            ViewBag.Brokers = new SelectList(await _db.Brokers.ToListAsync(), "Id", "Name");
            return View("Index");
        }

        const long maxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
        if (file.Length > maxFileSizeBytes)
        {
            ModelState.AddModelError(string.Empty, "File size must not exceed 10 MB.");
            ViewBag.Brokers = new SelectList(await _db.Brokers.ToListAsync(), "Id", "Name");
            return View("Index");
        }

        ImportPreviewViewModel preview;
        try
        {
            using var stream = file.OpenReadStream();
            preview = _importService.ParseExcelFile(stream, brokerId);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Failed to parse the file: {ex.Message}");
            ViewBag.Brokers = new SelectList(await _db.Brokers.ToListAsync(), "Id", "Name");
            return View("Index");
        }

        var json = JsonSerializer.Serialize(preview);
        HttpContext.Session.SetString(SessionKey, json);

        return RedirectToAction(nameof(Preview));
    }

    public async Task<IActionResult> Preview()
    {
        var json = HttpContext.Session.GetString(SessionKey);
        if (json == null) return RedirectToAction(nameof(Index));

        var preview = JsonSerializer.Deserialize<ImportPreviewViewModel>(json);
        if (preview == null) return RedirectToAction(nameof(Index));

        var broker = await _db.Brokers.FindAsync(preview.BrokerId);
        ViewBag.BrokerName = broker?.Name ?? "Unknown";

        return View(preview);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm()
    {
        var json = HttpContext.Session.GetString(SessionKey);
        if (json == null) return RedirectToAction(nameof(Index));

        var preview = JsonSerializer.Deserialize<ImportPreviewViewModel>(json);
        if (preview == null) return RedirectToAction(nameof(Index));

        HttpContext.Session.Remove(SessionKey);

        _db.CashTransactions.AddRange(preview.CashTransactions);
        _db.StockTrades.AddRange(preview.StockTrades);
        _db.MonthlyBalances.AddRange(preview.MonthlyBalances);
        await _db.SaveChangesAsync();

        TempData["ImportSuccess"] =
            $"Import complete: {preview.CashTransactions.Count} deposits/withdrawals, " +
            $"{preview.StockTrades.Count} stock trades, " +
            $"{preview.MonthlyBalances.Count} monthly balances.";

        return RedirectToAction("Index", "Dashboard");
    }
}
