using System.ComponentModel.DataAnnotations;

namespace PortfolioTracker.Models;

/// <summary>
/// Create/Edit form for a monthly balance. The S&amp;P 500 price is required here even though
/// it is stored separately (one row per month, shared by every broker), so that every month
/// with a balance can be valued on the benchmark at the same date.
/// </summary>
public class MonthlyBalanceFormViewModel
{
    public int Id { get; set; }

    [Range(2000, 2100, ErrorMessage = "Year must be between 2000 and 2100.")]
    public int Year { get; set; }

    [Range(1, 12, ErrorMessage = "Month must be between 1 and 12.")]
    public int Month { get; set; }

    [Range(0, 1000000000, ErrorMessage = "Balance cannot be negative.")]
    public decimal Balance { get; set; }

    public int BrokerId { get; set; }

    [Required(ErrorMessage = "The S&P 500 price is required.")]
    [Range(0.0001, 1000000, ErrorMessage = "The S&P 500 price must be greater than 0.")]
    public decimal? SP500Price { get; set; }

    public static MonthlyBalanceFormViewModel FromEntity(MonthlyBalance balance, decimal? sp500Price) => new()
    {
        Id = balance.Id,
        Year = balance.Year,
        Month = balance.Month,
        Balance = balance.Balance,
        BrokerId = balance.BrokerId,
        SP500Price = sp500Price
    };

    public void ApplyTo(MonthlyBalance balance)
    {
        balance.Year = Year;
        balance.Month = Month;
        balance.Balance = Balance;
        balance.BrokerId = BrokerId;
    }
}
