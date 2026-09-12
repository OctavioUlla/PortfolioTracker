using System.ComponentModel.DataAnnotations;

namespace PortfolioTracker.Models;

/// <summary>
/// Add form for a watch list entry. The ticker is normalised to upper case here so that the
/// unique index — and the upsert the MCP tool does — treat "aapl" and "AAPL" as one stock.
/// </summary>
public class StockWatchListItemFormViewModel
{
    [Required(ErrorMessage = "The ticker is required.")]
    [StringLength(10, ErrorMessage = "The ticker cannot be longer than 10 characters.")]
    public string Ticker { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "The name cannot be longer than 100 characters.")]
    public string? Name { get; set; }

    [Required(ErrorMessage = "The target buy price is required.")]
    [Range(0.0001, 1000000, ErrorMessage = "The target buy price must be greater than 0.")]
    public decimal? TargetPrice { get; set; }

    [StringLength(500, ErrorMessage = "The notes cannot be longer than 500 characters.")]
    public string? Notes { get; set; }

    public static string Normalize(string ticker) => ticker.Trim().ToUpperInvariant();

    public void ApplyTo(StockWatchListItem item)
    {
        item.Ticker = Normalize(Ticker);
        item.Name = string.IsNullOrWhiteSpace(Name) ? null : Name.Trim();
        item.TargetPrice = TargetPrice ?? 0;
        item.Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim();
    }
}
