# PortfolioTracker MCP Server

An [MCP (Model Context Protocol)](https://modelcontextprotocol.io/) server that lets AI assistants like **Claude** connect to your PortfolioTracker database and read or update your investment data.

## Available Tools

| Tool | Description |
|------|-------------|
| `GetPortfolioSummary` | Full portfolio overview: current value, lifetime IRR, total return (% & amount), net deposits, cash, and stock holdings |
| `GetStockHoldings` | Current stock positions with FIFO cost basis, average buy price, and holding period |
| `GetTransactions` | List deposits/withdrawals (filterable by type, broker, limit) |
| `RegisterDeposit` | Add a deposit to the portfolio |
| `RegisterWithdrawal` | Add a withdrawal from the portfolio |
| `GetStockTrades` | List stock trades (filterable by ticker, type, broker, limit) |
| `RegisterStockTrade` | Register a buy or sell trade |
| `GetLiquidityAccounts` | List cash/liquidity accounts with current balances and recent movements |
| `RegisterCashMovement` | Add a cash movement to a liquidity account (positive = deposit, negative = withdrawal) |
| `GetBrokers` | List all registered brokers |
| `GetMonthlyBalances` | List monthly portfolio balance records (filterable by broker, year) |
| `RegisterMonthlyBalance` | Add or update a monthly balance (upserts by year/month/broker) |

## Building

```bash
cd PortfolioTracker.McpServer
dotnet build
```

To produce a self-contained executable:

```bash
dotnet publish --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true
```

Replace `win-x64` with `linux-x64` or `osx-arm64` as appropriate for your platform.

## Configuration

The server reads the database path from the `PORTFOLIO_DB_PATH` environment variable. If not set it falls back to `portfolio.db` in the current working directory.

The main PortfolioTracker application stores its database at:

```
<app install folder>/portfolio.db
```

Set `PORTFOLIO_DB_PATH` to that full path when configuring your MCP client.

## Claude Desktop Setup

1. Build the MCP server (see above).
2. Open your Claude Desktop configuration file:
   - **macOS**: `~/Library/Application Support/Claude/claude_desktop_config.json`
   - **Windows**: `%APPDATA%\Claude\claude_desktop_config.json`
3. Add the server entry:

```json
{
  "mcpServers": {
    "portfolio-tracker": {
      "command": "/absolute/path/to/PortfolioTracker.McpServer",
      "env": {
        "PORTFOLIO_DB_PATH": "/absolute/path/to/portfolio.db"
      }
    }
  }
}
```

> **Windows example**
> ```json
> {
>   "mcpServers": {
>     "portfolio-tracker": {
>       "command": "C:\\PortfolioTracker\\PortfolioTracker.McpServer.exe",
>       "env": {
>         "PORTFOLIO_DB_PATH": "C:\\PortfolioTracker\\portfolio.db"
>       }
>     }
>   }
> }
> ```

4. Restart Claude Desktop. You should see the PortfolioTracker tools available in the tool panel.

## Usage Examples

Once connected, you can ask Claude:

- *"What is my current portfolio value and IRR?"*
- *"Show me my current stock holdings."*
- *"Register a deposit of $5,000 on 2024-03-15 with S&P 500 price 5,150."*
- *"Add a buy trade: 10 shares of AAPL at $175.50 on 2024-03-15."*
- *"What was my total return last year?"*
- *"Register a cash movement of -$1,000 in my savings account."*
