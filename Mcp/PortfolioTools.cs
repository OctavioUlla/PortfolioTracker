using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using PortfolioTracker.Data;
using PortfolioTracker.Models;
using PortfolioTracker.Services;

namespace PortfolioTracker.Mcp;

[McpServerToolType]
public class PortfolioTools
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly AppDbContext _db;

    public PortfolioTools(AppDbContext db) => _db = db;

    // -------------------------------------------------------------------------
    // Portfolio summary
    // -------------------------------------------------------------------------

    [McpServerTool]
    [Description("Get a comprehensive portfolio summary: current portfolio value, lifetime IRR, total return (% and amount), net deposits, total cash in liquidity accounts, and a breakdown of current stock holdings.")]
    public async Task<string> GetPortfolioSummary()
    {
        var monthlyBalances = await _db.MonthlyBalances.OrderBy(m => m.Year).ThenBy(m => m.Month).ToListAsync();
        var cashTransactions = await _db.CashTransactions.OrderBy(t => t.Date).ToListAsync();
        var stockTrades = await _db.StockTrades.OrderBy(t => t.Date).ToListAsync();
        var liquidityAccounts = await _db.LiquidityAccounts.Include(a => a.Movements).ToListAsync();

        var currentBalance = monthlyBalances
            .GroupBy(m => new { m.Year, m.Month })
            .OrderByDescending(g => g.Key.Year).ThenByDescending(g => g.Key.Month)
            .FirstOrDefault()?.Sum(m => m.Balance) ?? 0;

        var totalCash = liquidityAccounts.Sum(a => a.Movements.Sum(m => m.Amount));
        var holdings = StockHoldingsCalculator.Calculate(stockTrades);
        var irr = IrrCalculator.Calculate(cashTransactions, monthlyBalances);
        var totalReturn = TotalReturnCalculator.Calculate(cashTransactions, monthlyBalances);
        var totalReturnAmount = TotalReturnCalculator.CalculateAmount(cashTransactions, monthlyBalances);
        var netDeposits = cashTransactions.Sum(t => t.Type == TransactionType.Deposit ? t.Amount : -t.Amount);

        var result = new
        {
            PortfolioValue = currentBalance,
            TotalCash = totalCash,
            TotalAssets = currentBalance + totalCash,
            NetDeposits = netDeposits,
            LifetimeIRR_Percent = irr,
            TotalReturn_Percent = totalReturn,
            TotalReturn_Amount = totalReturnAmount,
            ActiveStockPositions = holdings.Count,
            StockHoldings = holdings.Select(h => new
            {
                h.Ticker,
                h.SharesHeld,
                h.AverageBuyPrice,
                h.TotalCost,
                AverageHoldingDays = Math.Round(h.AverageHoldingDays, 0)
            })
        };

        return JsonSerializer.Serialize(result, JsonOptions);
    }

    // -------------------------------------------------------------------------
    // Stock holdings
    // -------------------------------------------------------------------------

    [McpServerTool]
    [Description("Get current stock holdings with FIFO cost basis: ticker, shares held, average buy price, total cost, and average holding period in days.")]
    public async Task<string> GetStockHoldings()
    {
        var stockTrades = await _db.StockTrades.OrderBy(t => t.Date).ToListAsync();
        var holdings = StockHoldingsCalculator.Calculate(stockTrades);

        var result = holdings.Select(h => new
        {
            h.Ticker,
            h.SharesHeld,
            h.AverageBuyPrice,
            h.TotalCost,
            AverageHoldingDays = Math.Round(h.AverageHoldingDays, 0)
        });

        return JsonSerializer.Serialize(result, JsonOptions);
    }

    // -------------------------------------------------------------------------
    // Cash transactions (deposits / withdrawals)
    // -------------------------------------------------------------------------

    [McpServerTool]
    [Description("List cash transactions (deposits and withdrawals). Optionally filter by type ('deposit', 'withdrawal', or 'all') and/or by broker ID. Returns the most recent records up to the specified limit.")]
    public async Task<string> GetTransactions(
        [Description("Filter by type: 'deposit', 'withdrawal', or 'all' (default).")] string type = "all",
        [Description("Optional broker ID to filter by.")] int? brokerId = null,
        [Description("Maximum number of records to return (default 50).")] int limit = 50)
    {
        var query = _db.CashTransactions
            .Include(t => t.Broker)
            .AsQueryable();

        if (type.Equals("deposit", StringComparison.OrdinalIgnoreCase))
            query = query.Where(t => t.Type == TransactionType.Deposit);
        else if (type.Equals("withdrawal", StringComparison.OrdinalIgnoreCase))
            query = query.Where(t => t.Type == TransactionType.Withdrawal);

        if (brokerId.HasValue)
            query = query.Where(t => t.BrokerId == brokerId.Value);

        var transactions = await query
            .OrderByDescending(t => t.Date)
            .Take(limit)
            .ToListAsync();

        var result = transactions.Select(t => new
        {
            t.Id,
            Date = t.Date.ToString("yyyy-MM-dd"),
            Type = t.Type.ToString(),
            t.Amount,
            t.SP500Price,
            t.Notes,
            t.BrokerId,
            BrokerName = t.Broker?.Name
        });

        return JsonSerializer.Serialize(result, JsonOptions);
    }

    [McpServerTool]
    [Description("Register a deposit into the portfolio. The SP500 price on the deposit date is used to track virtual S&P 500 comparison performance.")]
    public async Task<string> RegisterDeposit(
        [Description("Date of the deposit in ISO format (yyyy-MM-dd).")] string date,
        [Description("Deposit amount.")] decimal amount,
        [Description("S&P 500 closing price on the deposit date (used for benchmark comparison).")] decimal sp500Price,
        [Description("Broker ID (use get_brokers to list available brokers; default broker ID is 1).")] int brokerId = 1,
        [Description("Optional notes.")] string? notes = null)
    {
        if (!DateTime.TryParse(date, out var parsedDate))
            return $"Error: invalid date '{date}'. Use ISO format yyyy-MM-dd.";
        if (amount <= 0)
            return "Error: amount must be greater than zero.";
        if (!await _db.Brokers.AnyAsync(b => b.Id == brokerId))
            return $"Error: broker with ID {brokerId} not found. Use get_brokers to list available brokers.";

        var transaction = new CashTransaction
        {
            Date = parsedDate,
            Type = TransactionType.Deposit,
            Amount = amount,
            SP500Price = sp500Price,
            Notes = notes,
            BrokerId = brokerId
        };

        _db.CashTransactions.Add(transaction);
        await _db.SaveChangesAsync();

        return JsonSerializer.Serialize(new
        {
            Success = true,
            Message = $"Deposit of {amount:C} registered on {parsedDate:yyyy-MM-dd}.",
            TransactionId = transaction.Id
        }, JsonOptions);
    }

    [McpServerTool]
    [Description("Register a withdrawal from the portfolio. The SP500 price on the withdrawal date is used to track virtual S&P 500 comparison performance.")]
    public async Task<string> RegisterWithdrawal(
        [Description("Date of the withdrawal in ISO format (yyyy-MM-dd).")] string date,
        [Description("Withdrawal amount.")] decimal amount,
        [Description("S&P 500 closing price on the withdrawal date (used for benchmark comparison).")] decimal sp500Price,
        [Description("Broker ID (use get_brokers to list available brokers; default broker ID is 1).")] int brokerId = 1,
        [Description("Optional notes.")] string? notes = null)
    {
        if (!DateTime.TryParse(date, out var parsedDate))
            return $"Error: invalid date '{date}'. Use ISO format yyyy-MM-dd.";
        if (amount <= 0)
            return "Error: amount must be greater than zero.";
        if (!await _db.Brokers.AnyAsync(b => b.Id == brokerId))
            return $"Error: broker with ID {brokerId} not found. Use get_brokers to list available brokers.";

        var transaction = new CashTransaction
        {
            Date = parsedDate,
            Type = TransactionType.Withdrawal,
            Amount = amount,
            SP500Price = sp500Price,
            Notes = notes,
            BrokerId = brokerId
        };

        _db.CashTransactions.Add(transaction);
        await _db.SaveChangesAsync();

        return JsonSerializer.Serialize(new
        {
            Success = true,
            Message = $"Withdrawal of {amount:C} registered on {parsedDate:yyyy-MM-dd}.",
            TransactionId = transaction.Id
        }, JsonOptions);
    }

    // -------------------------------------------------------------------------
    // Stock trades
    // -------------------------------------------------------------------------

    [McpServerTool]
    [Description("List stock trades. Optionally filter by ticker symbol, trade type ('buy', 'sell', or 'all'), and/or broker ID. Returns the most recent records up to the specified limit.")]
    public async Task<string> GetStockTrades(
        [Description("Optional ticker symbol to filter by (case-insensitive).")] string? ticker = null,
        [Description("Filter by type: 'buy', 'sell', or 'all' (default).")] string type = "all",
        [Description("Optional broker ID to filter by.")] int? brokerId = null,
        [Description("Maximum number of records to return (default 50).")] int limit = 50)
    {
        var query = _db.StockTrades
            .Include(t => t.Broker)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(ticker))
            query = query.Where(t => t.Ticker == ticker.ToUpperInvariant());

        if (type.Equals("buy", StringComparison.OrdinalIgnoreCase))
            query = query.Where(t => t.Type == TradeType.Buy);
        else if (type.Equals("sell", StringComparison.OrdinalIgnoreCase))
            query = query.Where(t => t.Type == TradeType.Sell);

        if (brokerId.HasValue)
            query = query.Where(t => t.BrokerId == brokerId.Value);

        var trades = await query
            .OrderByDescending(t => t.Date)
            .Take(limit)
            .ToListAsync();

        var result = trades.Select(t => new
        {
            t.Id,
            Date = t.Date.ToString("yyyy-MM-dd"),
            Type = t.Type.ToString(),
            t.Ticker,
            t.Quantity,
            t.Price,
            t.Commission,
            TotalValue = t.TotalValue,
            t.Notes,
            t.BrokerId,
            BrokerName = t.Broker?.Name
        });

        return JsonSerializer.Serialize(result, JsonOptions);
    }

    [McpServerTool]
    [Description("Register a stock trade (buy or sell). Ticker symbols are stored in upper case.")]
    public async Task<string> RegisterStockTrade(
        [Description("Date of the trade in ISO format (yyyy-MM-dd).")] string date,
        [Description("Trade type: 'buy' or 'sell'.")] string type,
        [Description("Ticker symbol (e.g. 'AAPL', 'MSFT').")] string ticker,
        [Description("Number of shares traded.")] decimal quantity,
        [Description("Price per share.")] decimal price,
        [Description("Broker ID (use get_brokers to list available brokers; default broker ID is 1).")] int brokerId = 1,
        [Description("Commission/fees paid for the trade (default 0).")] decimal commission = 0,
        [Description("Optional notes.")] string? notes = null)
    {
        if (!DateTime.TryParse(date, out var parsedDate))
            return $"Error: invalid date '{date}'. Use ISO format yyyy-MM-dd.";

        TradeType tradeType;
        if (type.Equals("buy", StringComparison.OrdinalIgnoreCase))
            tradeType = TradeType.Buy;
        else if (type.Equals("sell", StringComparison.OrdinalIgnoreCase))
            tradeType = TradeType.Sell;
        else
            return "Error: type must be 'buy' or 'sell'.";

        if (string.IsNullOrWhiteSpace(ticker))
            return "Error: ticker symbol is required.";
        if (quantity <= 0)
            return "Error: quantity must be greater than zero.";
        if (price <= 0)
            return "Error: price must be greater than zero.";
        if (commission < 0)
            return "Error: commission cannot be negative.";
        if (!await _db.Brokers.AnyAsync(b => b.Id == brokerId))
            return $"Error: broker with ID {brokerId} not found. Use get_brokers to list available brokers.";

        var trade = new StockTrade
        {
            Date = parsedDate,
            Type = tradeType,
            Ticker = ticker.ToUpperInvariant(),
            Quantity = quantity,
            Price = price,
            Commission = commission,
            Notes = notes,
            BrokerId = brokerId
        };

        _db.StockTrades.Add(trade);
        await _db.SaveChangesAsync();

        return JsonSerializer.Serialize(new
        {
            Success = true,
            Message = $"{tradeType} of {quantity} {ticker.ToUpperInvariant()} @ {price:C} registered on {parsedDate:yyyy-MM-dd}.",
            TradeId = trade.Id,
            TotalValue = trade.TotalValue
        }, JsonOptions);
    }

    // -------------------------------------------------------------------------
    // Liquidity accounts & cash movements
    // -------------------------------------------------------------------------

    [McpServerTool]
    [Description("List all liquidity (cash) accounts with their current balance (sum of all movements) and recent movement history.")]
    public async Task<string> GetLiquidityAccounts()
    {
        var accounts = await _db.LiquidityAccounts
            .Include(a => a.Movements)
            .ToListAsync();

        var result = accounts.Select(a => new
        {
            a.Id,
            a.Name,
            a.Notes,
            CurrentBalance = a.Movements.Sum(m => m.Amount),
            RecentMovements = a.Movements
                .OrderByDescending(m => m.Date)
                .Take(5)
                .Select(m => new
                {
                    m.Id,
                    Date = m.Date.ToString("yyyy-MM-dd"),
                    m.Amount,
                    m.Notes
                })
        });

        return JsonSerializer.Serialize(result, JsonOptions);
    }

    [McpServerTool]
    [Description("Register a cash movement (deposit or withdrawal) in a liquidity account. Use a positive amount for deposits and a negative amount for withdrawals.")]
    public async Task<string> RegisterCashMovement(
        [Description("Liquidity account ID (use get_liquidity_accounts to list available accounts).")] int liquidityAccountId,
        [Description("Date of the movement in ISO format (yyyy-MM-dd).")] string date,
        [Description("Amount of the movement (positive for deposit, negative for withdrawal).")] decimal amount,
        [Description("Optional notes.")] string? notes = null)
    {
        if (!DateTime.TryParse(date, out var parsedDate))
            return $"Error: invalid date '{date}'. Use ISO format yyyy-MM-dd.";
        if (amount == 0)
            return "Error: amount cannot be zero.";
        if (!await _db.LiquidityAccounts.AnyAsync(a => a.Id == liquidityAccountId))
            return $"Error: liquidity account with ID {liquidityAccountId} not found. Use get_liquidity_accounts to list available accounts.";

        var movement = new LiquidityMovement
        {
            LiquidityAccountId = liquidityAccountId,
            Date = parsedDate,
            Amount = amount,
            Notes = notes
        };

        _db.LiquidityMovements.Add(movement);
        await _db.SaveChangesAsync();

        return JsonSerializer.Serialize(new
        {
            Success = true,
            Message = $"Cash movement of {amount:+0.##;-0.##} registered on {parsedDate:yyyy-MM-dd} in account {liquidityAccountId}.",
            MovementId = movement.Id
        }, JsonOptions);
    }

    // -------------------------------------------------------------------------
    // Brokers
    // -------------------------------------------------------------------------

    [McpServerTool]
    [Description("List all brokers registered in the portfolio tracker.")]
    public async Task<string> GetBrokers()
    {
        var brokers = await _db.Brokers.ToListAsync();

        var result = brokers.Select(b => new
        {
            b.Id,
            b.Name
        });

        return JsonSerializer.Serialize(result, JsonOptions);
    }

    // -------------------------------------------------------------------------
    // Monthly balances
    // -------------------------------------------------------------------------

    [McpServerTool]
    [Description("List monthly portfolio balance records. Optionally filter by broker ID and/or year.")]
    public async Task<string> GetMonthlyBalances(
        [Description("Optional broker ID to filter by.")] int? brokerId = null,
        [Description("Optional year to filter by (e.g. 2024).")] int? year = null)
    {
        var query = _db.MonthlyBalances
            .Include(m => m.Broker)
            .AsQueryable();

        if (brokerId.HasValue)
            query = query.Where(m => m.BrokerId == brokerId.Value);

        if (year.HasValue)
            query = query.Where(m => m.Year == year.Value);

        var balances = await query
            .OrderBy(m => m.Year).ThenBy(m => m.Month)
            .ToListAsync();

        var result = balances.Select(m => new
        {
            m.Id,
            m.Year,
            m.Month,
            MonthName = m.MonthName,
            m.Balance,
            m.BrokerId,
            BrokerName = m.Broker?.Name
        });

        return JsonSerializer.Serialize(result, JsonOptions);
    }

    [McpServerTool]
    [Description("Register or update a monthly portfolio balance for a specific broker. If a record already exists for that year/month/broker combination it will be updated; otherwise a new record is created.")]
    public async Task<string> RegisterMonthlyBalance(
        [Description("Year (e.g. 2024).")] int year,
        [Description("Month number (1-12).")] int month,
        [Description("Portfolio balance at the end of the month.")] decimal balance,
        [Description("Broker ID (use get_brokers to list available brokers; default broker ID is 1).")] int brokerId = 1)
    {
        if (year < 1900 || year > 2100)
            return "Error: year must be between 1900 and 2100.";
        if (month < 1 || month > 12)
            return "Error: month must be between 1 and 12.";
        if (balance < 0)
            return "Error: balance cannot be negative.";
        if (!await _db.Brokers.AnyAsync(b => b.Id == brokerId))
            return $"Error: broker with ID {brokerId} not found. Use get_brokers to list available brokers.";

        var existing = await _db.MonthlyBalances
            .FirstOrDefaultAsync(m => m.Year == year && m.Month == month && m.BrokerId == brokerId);

        bool isUpdate = existing != null;
        decimal oldBalance = existing?.Balance ?? 0;

        if (existing != null)
        {
            existing.Balance = balance;
        }
        else
        {
            existing = new MonthlyBalance
            {
                Year = year,
                Month = month,
                Balance = balance,
                BrokerId = brokerId
            };
            _db.MonthlyBalances.Add(existing);
        }

        await _db.SaveChangesAsync();

        return JsonSerializer.Serialize(new
        {
            Success = true,
            Action = isUpdate ? "updated" : "created",
            Message = isUpdate
                ? $"Monthly balance for {new DateTime(year, month, 1):MMMM yyyy} updated from {oldBalance:C} to {balance:C}."
                : $"Monthly balance of {balance:C} registered for {new DateTime(year, month, 1):MMMM yyyy}.",
            BalanceId = existing.Id
        }, JsonOptions);
    }
}
