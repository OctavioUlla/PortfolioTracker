namespace PortfolioTracker.Models;

public class MonthlyBalance
{
    public int Id { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Balance { get; set; }
    public int BrokerId { get; set; }
    public Broker? Broker { get; set; }

    public DateTime Date => new DateTime(Year, Month, 1);
    public string MonthName => Date.ToString("MMMM yyyy");
}
