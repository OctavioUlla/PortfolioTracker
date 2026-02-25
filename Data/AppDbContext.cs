using Microsoft.EntityFrameworkCore;
using PortfolioTracker.Models;

namespace PortfolioTracker.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Broker> Brokers { get; set; }
    public DbSet<CashTransaction> CashTransactions { get; set; }
    public DbSet<StockTrade> StockTrades { get; set; }
    public DbSet<MonthlyBalance> MonthlyBalances { get; set; }
    public DbSet<LiquidityAccount> LiquidityAccounts { get; set; }
    public DbSet<LiquidityMovement> LiquidityMovements { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Broker>().HasData(
            new Broker { Id = 1, Name = "Default Broker" }
        );

        modelBuilder.Entity<CashTransaction>()
            .Property(t => t.Amount).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<CashTransaction>()
            .Property(t => t.SP500Price).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<StockTrade>()
            .Property(t => t.Price).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<StockTrade>()
            .Property(t => t.Quantity).HasColumnType("decimal(18,6)");
        modelBuilder.Entity<StockTrade>()
            .Property(t => t.Commission).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<MonthlyBalance>()
            .Property(t => t.Balance).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<LiquidityMovement>()
            .Property(t => t.Amount).HasColumnType("decimal(18,2)");
    }
}
