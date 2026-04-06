using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PortfolioTracker.Data;
using PortfolioTracker.Mcp;
using PortfolioTracker.Models;

namespace PortfolioTracker.McpServer.Tests;

[TestFixture]
public class McpToolsTests
{
    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);
        // Seed the default broker that tools rely on (mirrors the EF seed data)
        context.Brokers.Add(new Broker { Id = 1, Name = "Default Broker" });
        context.SaveChanges();
        return context;
    }

    // -------------------------------------------------------------------------
    // GetBrokers
    // -------------------------------------------------------------------------

    [Test]
    public async Task GetBrokers_ReturnsSeededBroker()
    {
        using var db = CreateContext();
        var tools = new PortfolioTools(db);

        var json = await tools.GetBrokers();

        var doc = JsonDocument.Parse(json);
        var brokers = doc.RootElement.EnumerateArray().ToList();
        Assert.That(brokers, Has.Count.EqualTo(1));
        Assert.That(brokers[0].GetProperty("Id").GetInt32(), Is.EqualTo(1));
        Assert.That(brokers[0].GetProperty("Name").GetString(), Is.EqualTo("Default Broker"));
    }

    // -------------------------------------------------------------------------
    // GetPortfolioSummary
    // -------------------------------------------------------------------------

    [Test]
    public async Task GetPortfolioSummary_EmptyDatabase_ReturnsZeros()
    {
        using var db = CreateContext();
        var tools = new PortfolioTools(db);

        var json = await tools.GetPortfolioSummary();

        var root = JsonDocument.Parse(json).RootElement;
        Assert.That(root.GetProperty("PortfolioValue").GetDecimal(), Is.EqualTo(0));
        Assert.That(root.GetProperty("TotalCash").GetDecimal(), Is.EqualTo(0));
        Assert.That(root.GetProperty("NetDeposits").GetDecimal(), Is.EqualTo(0));
        Assert.That(root.GetProperty("ActiveStockPositions").GetInt32(), Is.EqualTo(0));
    }

    [Test]
    public async Task GetPortfolioSummary_WithData_ReturnsCorrectValues()
    {
        using var db = CreateContext();
        db.CashTransactions.Add(new CashTransaction
            { Date = new DateTime(2024, 1, 1), Type = TransactionType.Deposit, Amount = 10000, SP500Price = 4800, BrokerId = 1 });
        db.MonthlyBalances.Add(new MonthlyBalance { Year = 2024, Month = 1, Balance = 11000, BrokerId = 1 });
        await db.SaveChangesAsync();

        var tools = new PortfolioTools(db);
        var json = await tools.GetPortfolioSummary();

        var root = JsonDocument.Parse(json).RootElement;
        Assert.That(root.GetProperty("PortfolioValue").GetDecimal(), Is.EqualTo(11000));
        Assert.That(root.GetProperty("NetDeposits").GetDecimal(), Is.EqualTo(10000));
        Assert.That(root.GetProperty("TotalReturn_Amount").GetDecimal(), Is.EqualTo(1000));
    }

    // -------------------------------------------------------------------------
    // GetStockHoldings
    // -------------------------------------------------------------------------

    [Test]
    public async Task GetStockHoldings_EmptyDatabase_ReturnsEmptyArray()
    {
        using var db = CreateContext();
        var tools = new PortfolioTools(db);

        var json = await tools.GetStockHoldings();

        var holdings = JsonDocument.Parse(json).RootElement.EnumerateArray().ToList();
        Assert.That(holdings, Is.Empty);
    }

    [Test]
    public async Task GetStockHoldings_WithBuyTrade_ReturnsHolding()
    {
        using var db = CreateContext();
        db.StockTrades.Add(new StockTrade
            { Date = new DateTime(2024, 1, 10), Type = TradeType.Buy, Ticker = "AAPL", Quantity = 10, Price = 175, Commission = 0, BrokerId = 1 });
        await db.SaveChangesAsync();

        var tools = new PortfolioTools(db);
        var json = await tools.GetStockHoldings();

        var holdings = JsonDocument.Parse(json).RootElement.EnumerateArray().ToList();
        Assert.That(holdings, Has.Count.EqualTo(1));
        Assert.That(holdings[0].GetProperty("Ticker").GetString(), Is.EqualTo("AAPL"));
        Assert.That(holdings[0].GetProperty("SharesHeld").GetDecimal(), Is.EqualTo(10));
        Assert.That(holdings[0].GetProperty("AverageBuyPrice").GetDecimal(), Is.EqualTo(175));
    }

    [Test]
    public async Task GetStockHoldings_AfterFullSell_ReturnsEmptyArray()
    {
        using var db = CreateContext();
        db.StockTrades.AddRange(
            new StockTrade { Date = new DateTime(2024, 1, 1), Type = TradeType.Buy, Ticker = "TSLA", Quantity = 5, Price = 200, Commission = 0, BrokerId = 1 },
            new StockTrade { Date = new DateTime(2024, 2, 1), Type = TradeType.Sell, Ticker = "TSLA", Quantity = 5, Price = 250, Commission = 0, BrokerId = 1 }
        );
        await db.SaveChangesAsync();

        var tools = new PortfolioTools(db);
        var json = await tools.GetStockHoldings();

        var holdings = JsonDocument.Parse(json).RootElement.EnumerateArray().ToList();
        Assert.That(holdings, Is.Empty);
    }

    // -------------------------------------------------------------------------
    // GetTransactions
    // -------------------------------------------------------------------------

    [Test]
    public async Task GetTransactions_EmptyDatabase_ReturnsEmptyArray()
    {
        using var db = CreateContext();
        var tools = new PortfolioTools(db);

        var json = await tools.GetTransactions();

        var transactions = JsonDocument.Parse(json).RootElement.EnumerateArray().ToList();
        Assert.That(transactions, Is.Empty);
    }

    [Test]
    public async Task GetTransactions_FilterByDeposit_ReturnsOnlyDeposits()
    {
        using var db = CreateContext();
        db.CashTransactions.AddRange(
            new CashTransaction { Date = new DateTime(2024, 1, 1), Type = TransactionType.Deposit, Amount = 5000, SP500Price = 4800, BrokerId = 1 },
            new CashTransaction { Date = new DateTime(2024, 2, 1), Type = TransactionType.Withdrawal, Amount = 1000, SP500Price = 4900, BrokerId = 1 }
        );
        await db.SaveChangesAsync();

        var tools = new PortfolioTools(db);
        var json = await tools.GetTransactions(type: "deposit");

        var transactions = JsonDocument.Parse(json).RootElement.EnumerateArray().ToList();
        Assert.That(transactions, Has.Count.EqualTo(1));
        Assert.That(transactions[0].GetProperty("Type").GetString(), Is.EqualTo("Deposit"));
    }

    [Test]
    public async Task GetTransactions_LimitParameter_RespectsLimit()
    {
        using var db = CreateContext();
        for (int i = 1; i <= 10; i++)
            db.CashTransactions.Add(new CashTransaction
                { Date = new DateTime(2024, 1, i), Type = TransactionType.Deposit, Amount = 100 * i, SP500Price = 4800, BrokerId = 1 });
        await db.SaveChangesAsync();

        var tools = new PortfolioTools(db);
        var json = await tools.GetTransactions(limit: 3);

        var transactions = JsonDocument.Parse(json).RootElement.EnumerateArray().ToList();
        Assert.That(transactions, Has.Count.EqualTo(3));
    }

    // -------------------------------------------------------------------------
    // RegisterDeposit
    // -------------------------------------------------------------------------

    [Test]
    public async Task RegisterDeposit_ValidData_PersistsTransaction()
    {
        using var db = CreateContext();
        var tools = new PortfolioTools(db);

        var json = await tools.RegisterDeposit("2024-06-01", 3000, 5200, 1, "Test deposit");

        var root = JsonDocument.Parse(json).RootElement;
        Assert.That(root.GetProperty("Success").GetBoolean(), Is.True);
        Assert.That(root.GetProperty("TransactionId").GetInt32(), Is.GreaterThan(0));

        var saved = await db.CashTransactions.FindAsync(root.GetProperty("TransactionId").GetInt32());
        Assert.That(saved, Is.Not.Null);
        Assert.That(saved!.Amount, Is.EqualTo(3000));
        Assert.That(saved.Type, Is.EqualTo(TransactionType.Deposit));
        Assert.That(saved.SP500Price, Is.EqualTo(5200));
        Assert.That(saved.Notes, Is.EqualTo("Test deposit"));
    }

    [Test]
    public async Task RegisterDeposit_InvalidDate_ReturnsError()
    {
        using var db = CreateContext();
        var tools = new PortfolioTools(db);

        var result = await tools.RegisterDeposit("not-a-date", 1000, 5000);

        Assert.That(result, Does.StartWith("Error:"));
        Assert.That(await db.CashTransactions.CountAsync(), Is.EqualTo(0));
    }

    [Test]
    public async Task RegisterDeposit_ZeroAmount_ReturnsError()
    {
        using var db = CreateContext();
        var tools = new PortfolioTools(db);

        var result = await tools.RegisterDeposit("2024-01-01", 0, 5000);

        Assert.That(result, Does.StartWith("Error:"));
    }

    [Test]
    public async Task RegisterDeposit_InvalidBroker_ReturnsError()
    {
        using var db = CreateContext();
        var tools = new PortfolioTools(db);

        var result = await tools.RegisterDeposit("2024-01-01", 1000, 5000, brokerId: 999);

        Assert.That(result, Does.StartWith("Error:"));
    }

    // -------------------------------------------------------------------------
    // RegisterWithdrawal
    // -------------------------------------------------------------------------

    [Test]
    public async Task RegisterWithdrawal_ValidData_PersistsTransaction()
    {
        using var db = CreateContext();
        var tools = new PortfolioTools(db);

        var json = await tools.RegisterWithdrawal("2024-07-15", 2000, 5300, 1);

        var root = JsonDocument.Parse(json).RootElement;
        Assert.That(root.GetProperty("Success").GetBoolean(), Is.True);

        var saved = await db.CashTransactions.FindAsync(root.GetProperty("TransactionId").GetInt32());
        Assert.That(saved!.Type, Is.EqualTo(TransactionType.Withdrawal));
        Assert.That(saved.Amount, Is.EqualTo(2000));
    }

    [Test]
    public async Task RegisterWithdrawal_NegativeAmount_ReturnsError()
    {
        using var db = CreateContext();
        var tools = new PortfolioTools(db);

        var result = await tools.RegisterWithdrawal("2024-01-01", -500, 5000);

        Assert.That(result, Does.StartWith("Error:"));
    }

    // -------------------------------------------------------------------------
    // GetStockTrades
    // -------------------------------------------------------------------------

    [Test]
    public async Task GetStockTrades_EmptyDatabase_ReturnsEmptyArray()
    {
        using var db = CreateContext();
        var tools = new PortfolioTools(db);

        var json = await tools.GetStockTrades();

        var trades = JsonDocument.Parse(json).RootElement.EnumerateArray().ToList();
        Assert.That(trades, Is.Empty);
    }

    [Test]
    public async Task GetStockTrades_FilterByTicker_ReturnsMatchingTrades()
    {
        using var db = CreateContext();
        db.StockTrades.AddRange(
            new StockTrade { Date = new DateTime(2024, 1, 1), Type = TradeType.Buy, Ticker = "AAPL", Quantity = 10, Price = 175, Commission = 0, BrokerId = 1 },
            new StockTrade { Date = new DateTime(2024, 1, 2), Type = TradeType.Buy, Ticker = "MSFT", Quantity = 5, Price = 400, Commission = 0, BrokerId = 1 }
        );
        await db.SaveChangesAsync();

        var tools = new PortfolioTools(db);
        var json = await tools.GetStockTrades(ticker: "aapl");

        var trades = JsonDocument.Parse(json).RootElement.EnumerateArray().ToList();
        Assert.That(trades, Has.Count.EqualTo(1));
        Assert.That(trades[0].GetProperty("Ticker").GetString(), Is.EqualTo("AAPL"));
    }

    [Test]
    public async Task GetStockTrades_FilterBySellType_ReturnsOnlySells()
    {
        using var db = CreateContext();
        db.StockTrades.AddRange(
            new StockTrade { Date = new DateTime(2024, 1, 1), Type = TradeType.Buy, Ticker = "AAPL", Quantity = 10, Price = 175, Commission = 0, BrokerId = 1 },
            new StockTrade { Date = new DateTime(2024, 3, 1), Type = TradeType.Sell, Ticker = "AAPL", Quantity = 5, Price = 190, Commission = 0, BrokerId = 1 }
        );
        await db.SaveChangesAsync();

        var tools = new PortfolioTools(db);
        var json = await tools.GetStockTrades(type: "sell");

        var trades = JsonDocument.Parse(json).RootElement.EnumerateArray().ToList();
        Assert.That(trades, Has.Count.EqualTo(1));
        Assert.That(trades[0].GetProperty("Type").GetString(), Is.EqualTo("Sell"));
    }

    // -------------------------------------------------------------------------
    // RegisterStockTrade
    // -------------------------------------------------------------------------

    [Test]
    public async Task RegisterStockTrade_ValidBuy_PersistsTrade()
    {
        using var db = CreateContext();
        var tools = new PortfolioTools(db);

        var json = await tools.RegisterStockTrade("2024-04-01", "buy", "aapl", 10, 175.50m, 1, 0);

        var root = JsonDocument.Parse(json).RootElement;
        Assert.That(root.GetProperty("Success").GetBoolean(), Is.True);
        Assert.That(root.GetProperty("TotalValue").GetDecimal(), Is.EqualTo(1755));

        var saved = await db.StockTrades.FindAsync(root.GetProperty("TradeId").GetInt32());
        Assert.That(saved!.Ticker, Is.EqualTo("AAPL"));
        Assert.That(saved.Type, Is.EqualTo(TradeType.Buy));
        Assert.That(saved.Quantity, Is.EqualTo(10));
    }

    [Test]
    public async Task RegisterStockTrade_ValidSell_PersistsTrade()
    {
        using var db = CreateContext();
        var tools = new PortfolioTools(db);

        var json = await tools.RegisterStockTrade("2024-05-01", "sell", "TSLA", 3, 250m, 1, 1.5m);

        var root = JsonDocument.Parse(json).RootElement;
        Assert.That(root.GetProperty("Success").GetBoolean(), Is.True);

        var saved = await db.StockTrades.FindAsync(root.GetProperty("TradeId").GetInt32());
        Assert.That(saved!.Type, Is.EqualTo(TradeType.Sell));
        Assert.That(saved.Commission, Is.EqualTo(1.5m));
    }

    [Test]
    public async Task RegisterStockTrade_InvalidType_ReturnsError()
    {
        using var db = CreateContext();
        var tools = new PortfolioTools(db);

        var result = await tools.RegisterStockTrade("2024-01-01", "hold", "AAPL", 5, 100);

        Assert.That(result, Does.StartWith("Error:"));
    }

    [Test]
    public async Task RegisterStockTrade_EmptyTicker_ReturnsError()
    {
        using var db = CreateContext();
        var tools = new PortfolioTools(db);

        var result = await tools.RegisterStockTrade("2024-01-01", "buy", "", 5, 100);

        Assert.That(result, Does.StartWith("Error:"));
    }

    [Test]
    public async Task RegisterStockTrade_ZeroQuantity_ReturnsError()
    {
        using var db = CreateContext();
        var tools = new PortfolioTools(db);

        var result = await tools.RegisterStockTrade("2024-01-01", "buy", "AAPL", 0, 100);

        Assert.That(result, Does.StartWith("Error:"));
    }

    [Test]
    public async Task RegisterStockTrade_NegativeCommission_ReturnsError()
    {
        using var db = CreateContext();
        var tools = new PortfolioTools(db);

        var result = await tools.RegisterStockTrade("2024-01-01", "buy", "AAPL", 5, 100, commission: -1);

        Assert.That(result, Does.StartWith("Error:"));
    }

    // -------------------------------------------------------------------------
    // GetLiquidityAccounts
    // -------------------------------------------------------------------------

    [Test]
    public async Task GetLiquidityAccounts_EmptyDatabase_ReturnsEmptyArray()
    {
        using var db = CreateContext();
        var tools = new PortfolioTools(db);

        var json = await tools.GetLiquidityAccounts();

        var accounts = JsonDocument.Parse(json).RootElement.EnumerateArray().ToList();
        Assert.That(accounts, Is.Empty);
    }

    [Test]
    public async Task GetLiquidityAccounts_WithMovements_ReturnsCorrectBalance()
    {
        using var db = CreateContext();
        var account = new LiquidityAccount { Name = "Savings", Notes = "Emergency fund" };
        db.LiquidityAccounts.Add(account);
        await db.SaveChangesAsync();

        db.LiquidityMovements.AddRange(
            new LiquidityMovement { LiquidityAccountId = account.Id, Date = new DateTime(2024, 1, 1), Amount = 5000 },
            new LiquidityMovement { LiquidityAccountId = account.Id, Date = new DateTime(2024, 2, 1), Amount = -1000 }
        );
        await db.SaveChangesAsync();

        var tools = new PortfolioTools(db);
        var json = await tools.GetLiquidityAccounts();

        var accounts = JsonDocument.Parse(json).RootElement.EnumerateArray().ToList();
        Assert.That(accounts, Has.Count.EqualTo(1));
        Assert.That(accounts[0].GetProperty("CurrentBalance").GetDecimal(), Is.EqualTo(4000));
        Assert.That(accounts[0].GetProperty("Name").GetString(), Is.EqualTo("Savings"));
    }

    // -------------------------------------------------------------------------
    // RegisterCashMovement
    // -------------------------------------------------------------------------

    [Test]
    public async Task RegisterCashMovement_ValidPositiveAmount_PersistsMovement()
    {
        using var db = CreateContext();
        var account = new LiquidityAccount { Name = "Checking" };
        db.LiquidityAccounts.Add(account);
        await db.SaveChangesAsync();

        var tools = new PortfolioTools(db);
        var json = await tools.RegisterCashMovement(account.Id, "2024-03-01", 2500, "Salary");

        var root = JsonDocument.Parse(json).RootElement;
        Assert.That(root.GetProperty("Success").GetBoolean(), Is.True);
        Assert.That(root.GetProperty("MovementId").GetInt32(), Is.GreaterThan(0));

        var saved = await db.LiquidityMovements.FindAsync(root.GetProperty("MovementId").GetInt32());
        Assert.That(saved!.Amount, Is.EqualTo(2500));
        Assert.That(saved.Notes, Is.EqualTo("Salary"));
    }

    [Test]
    public async Task RegisterCashMovement_NegativeAmount_PersistsWithdrawal()
    {
        using var db = CreateContext();
        var account = new LiquidityAccount { Name = "Checking" };
        db.LiquidityAccounts.Add(account);
        await db.SaveChangesAsync();

        var tools = new PortfolioTools(db);
        var json = await tools.RegisterCashMovement(account.Id, "2024-03-15", -500);

        var root = JsonDocument.Parse(json).RootElement;
        Assert.That(root.GetProperty("Success").GetBoolean(), Is.True);

        var saved = await db.LiquidityMovements.FindAsync(root.GetProperty("MovementId").GetInt32());
        Assert.That(saved!.Amount, Is.EqualTo(-500));
    }

    [Test]
    public async Task RegisterCashMovement_ZeroAmount_ReturnsError()
    {
        using var db = CreateContext();
        var account = new LiquidityAccount { Name = "Checking" };
        db.LiquidityAccounts.Add(account);
        await db.SaveChangesAsync();

        var tools = new PortfolioTools(db);
        var result = await tools.RegisterCashMovement(account.Id, "2024-01-01", 0);

        Assert.That(result, Does.StartWith("Error:"));
    }

    [Test]
    public async Task RegisterCashMovement_InvalidAccount_ReturnsError()
    {
        using var db = CreateContext();
        var tools = new PortfolioTools(db);

        var result = await tools.RegisterCashMovement(9999, "2024-01-01", 1000);

        Assert.That(result, Does.StartWith("Error:"));
    }

    [Test]
    public async Task RegisterCashMovement_InvalidDate_ReturnsError()
    {
        using var db = CreateContext();
        var account = new LiquidityAccount { Name = "Checking" };
        db.LiquidityAccounts.Add(account);
        await db.SaveChangesAsync();

        var tools = new PortfolioTools(db);
        var result = await tools.RegisterCashMovement(account.Id, "invalid-date", 500);

        Assert.That(result, Does.StartWith("Error:"));
    }

    // -------------------------------------------------------------------------
    // GetMonthlyBalances
    // -------------------------------------------------------------------------

    [Test]
    public async Task GetMonthlyBalances_EmptyDatabase_ReturnsEmptyArray()
    {
        using var db = CreateContext();
        var tools = new PortfolioTools(db);

        var json = await tools.GetMonthlyBalances();

        var balances = JsonDocument.Parse(json).RootElement.EnumerateArray().ToList();
        Assert.That(balances, Is.Empty);
    }

    [Test]
    public async Task GetMonthlyBalances_FilterByYear_ReturnsOnlyMatchingYear()
    {
        using var db = CreateContext();
        db.MonthlyBalances.AddRange(
            new MonthlyBalance { Year = 2023, Month = 12, Balance = 8000, BrokerId = 1 },
            new MonthlyBalance { Year = 2024, Month = 1, Balance = 9000, BrokerId = 1 },
            new MonthlyBalance { Year = 2024, Month = 2, Balance = 9500, BrokerId = 1 }
        );
        await db.SaveChangesAsync();

        var tools = new PortfolioTools(db);
        var json = await tools.GetMonthlyBalances(year: 2024);

        var balances = JsonDocument.Parse(json).RootElement.EnumerateArray().ToList();
        Assert.That(balances, Has.Count.EqualTo(2));
        Assert.That(balances.All(b => b.GetProperty("Year").GetInt32() == 2024), Is.True);
    }

    // -------------------------------------------------------------------------
    // RegisterMonthlyBalance
    // -------------------------------------------------------------------------

    [Test]
    public async Task RegisterMonthlyBalance_NewRecord_CreatesBalance()
    {
        using var db = CreateContext();
        var tools = new PortfolioTools(db);

        var json = await tools.RegisterMonthlyBalance(2024, 3, 12000, 1);

        var root = JsonDocument.Parse(json).RootElement;
        Assert.That(root.GetProperty("Success").GetBoolean(), Is.True);
        Assert.That(root.GetProperty("Action").GetString(), Is.EqualTo("created"));

        var saved = await db.MonthlyBalances.FindAsync(root.GetProperty("BalanceId").GetInt32());
        Assert.That(saved!.Balance, Is.EqualTo(12000));
        Assert.That(saved.Year, Is.EqualTo(2024));
        Assert.That(saved.Month, Is.EqualTo(3));
    }

    [Test]
    public async Task RegisterMonthlyBalance_ExistingRecord_UpdatesBalance()
    {
        using var db = CreateContext();
        db.MonthlyBalances.Add(new MonthlyBalance { Year = 2024, Month = 6, Balance = 10000, BrokerId = 1 });
        await db.SaveChangesAsync();

        var tools = new PortfolioTools(db);
        var json = await tools.RegisterMonthlyBalance(2024, 6, 11500, 1);

        var root = JsonDocument.Parse(json).RootElement;
        Assert.That(root.GetProperty("Success").GetBoolean(), Is.True);
        Assert.That(root.GetProperty("Action").GetString(), Is.EqualTo("updated"));

        var balance = await db.MonthlyBalances.FirstAsync(m => m.Year == 2024 && m.Month == 6);
        Assert.That(balance.Balance, Is.EqualTo(11500));
    }

    [Test]
    public async Task RegisterMonthlyBalance_InvalidMonth_ReturnsError()
    {
        using var db = CreateContext();
        var tools = new PortfolioTools(db);

        var result = await tools.RegisterMonthlyBalance(2024, 13, 10000);

        Assert.That(result, Does.StartWith("Error:"));
    }

    [Test]
    public async Task RegisterMonthlyBalance_NegativeBalance_ReturnsError()
    {
        using var db = CreateContext();
        var tools = new PortfolioTools(db);

        var result = await tools.RegisterMonthlyBalance(2024, 1, -100);

        Assert.That(result, Does.StartWith("Error:"));
    }

    [Test]
    public async Task RegisterMonthlyBalance_InvalidBroker_ReturnsError()
    {
        using var db = CreateContext();
        var tools = new PortfolioTools(db);

        var result = await tools.RegisterMonthlyBalance(2024, 1, 10000, brokerId: 999);

        Assert.That(result, Does.StartWith("Error:"));
    }
}
