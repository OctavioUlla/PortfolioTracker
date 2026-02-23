namespace PortfolioTracker.Models;

public enum TransactionType { Deposit, Withdrawal }

public class CashTransaction
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal SP500Price { get; set; }
    public string? Notes { get; set; }
    public int BrokerId { get; set; }
    public Broker? Broker { get; set; }
}
