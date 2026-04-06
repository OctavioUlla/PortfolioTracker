# PortfolioTracker

A web-based investment portfolio tracker built with **ASP.NET Core 8**. Track your deposits, withdrawals, stock trades, and monthly balances across multiple brokers — with IRR calculation and S&P 500 strategy comparison.

![Dashboard](https://github.com/user-attachments/assets/3d575d18-5327-4392-a7ec-480fa414824a)

---

## Features

| Feature | Description |
|---------|-------------|
| 💰 **Deposits & Withdrawals** | Register cash flows with the S&P 500 price at the time of each transaction |
| 📈 **Stock Trades** | Record buy/sell trades with ticker, quantity, price, and commission |
| 🏦 **Monthly Balances** | Register end-of-month account balances per broker |
| 📊 **Portfolio Chart** | Line chart comparing your portfolio value vs a virtual S&P 500 portfolio over time |
| 📐 **IRR / XIRR Calculator** | Annualized rate of return for the lifetime, a specific year, or a custom date range |
| 🆚 **S&P 500 Comparison** | A virtual portfolio that automatically buys/sells S&P 500 units on every deposit/withdrawal |
| 🏢 **Multiple Brokers** | All data is broker-scoped; totals are aggregated on the dashboard |

---

## Tech Stack

- **Backend**: ASP.NET Core 8 MVC, Entity Framework Core 8
- **Database**: SQLite (auto-migrated on startup, no server required)
- **Frontend**: Bootstrap 5, Chart.js 4, Font Awesome 6 (all via CDN)

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Run locally

```bash
git clone https://github.com/OctavioUlla/PortfolioTracker.git
cd PortfolioTracker
dotnet run
```

The app will be available at `http://localhost:5285`. The SQLite database (`portfolio.db`) is created and migrated automatically on first run.

### First steps

1. Go to **Settings → Brokers** to add your brokerage accounts.
2. Go to **Deposits & Withdrawals** to record your cash flows (include the S&P 500 price for strategy comparison).
3. Go to **Monthly Balances** to record your end-of-month account values.
4. Go to **Stock Trades** to log your individual buy/sell operations.
5. Visit the **Dashboard** to see your portfolio chart, IRR, and S&P 500 comparison.

---

## Project Structure

```
PortfolioTracker/
├── Controllers/          # MVC controllers (Dashboard, Transactions, StockTrades, MonthlyBalances, Brokers)
├── Data/                 # EF Core DbContext
├── Migrations/           # EF Core database migrations
├── Models/               # Domain models and view models
├── Services/             # IRR (XIRR) calculator, S&P 500 virtual portfolio calculator
├── Views/                # Razor views (Bootstrap 5 sidebar layout)
└── wwwroot/              # Static assets
```

---

## Download

The latest Windows release is available on the [Releases](https://github.com/OctavioUlla/PortfolioTracker/releases/latest) page.

1. Download `portfolio-tracker-*-win-x64.zip` from the latest release.
2. Extract the zip.
3. Run `PortfolioTracker.exe`.
4. The app opens at `http://localhost:5285`. The SQLite database is created automatically.

---

## MCP Server (AI Integration)

The `PortfolioTracker.McpServer/` project exposes all portfolio data and operations as [MCP (Model Context Protocol)](https://modelcontextprotocol.io/) tools, so AI assistants like **Claude** can read and update your portfolio directly.

### Available Tools

| Tool | Description |
|------|-------------|
| `GetPortfolioSummary` | Current value, lifetime IRR, total return (% & amount), net deposits, total cash, and stock holdings |
| `GetStockHoldings` | Current positions with FIFO cost basis and average holding period |
| `GetTransactions` | List deposits/withdrawals (filterable by type and broker) |
| `RegisterDeposit` | Add a deposit (include the S&P 500 price for benchmark comparison) |
| `RegisterWithdrawal` | Add a withdrawal |
| `GetStockTrades` | List stock trades (filterable by ticker, type, and broker) |
| `RegisterStockTrade` | Register a buy or sell trade |
| `GetLiquidityAccounts` | List cash accounts with current balances and recent movements |
| `RegisterCashMovement` | Add a cash movement (positive = deposit, negative = withdrawal) |
| `GetBrokers` | List all registered brokers |
| `GetMonthlyBalances` | List monthly portfolio balance records (filterable by broker and year) |
| `RegisterMonthlyBalance` | Add or update a monthly balance (upserts by year/month/broker) |

### Building

```bash
cd PortfolioTracker.McpServer
dotnet build
```

To produce a self-contained executable:

```bash
dotnet publish --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true
```

Replace `win-x64` with `linux-x64` or `osx-arm64` as appropriate for your platform.

### Configuration

Set the `PORTFOLIO_DB_PATH` environment variable to the full path of your `portfolio.db` file. If not set, the server looks for `portfolio.db` in the current working directory.

### Claude Desktop Setup

1. Build or download the MCP server executable.
2. Open your Claude Desktop configuration file:
   - **macOS**: `~/Library/Application Support/Claude/claude_desktop_config.json`
   - **Windows**: `%APPDATA%\Claude\claude_desktop_config.json`
3. Add the following entry (adjust the paths for your system):

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

4. Restart Claude Desktop. The PortfolioTracker tools will appear in the tool panel.

### Example Prompts

- *"What is my current portfolio value and IRR?"*
- *"Show me my current stock holdings."*
- *"Register a deposit of $5,000 on 2024-03-15 with S&P 500 price 5,150."*
- *"Add a buy trade: 10 shares of AAPL at $175.50 on 2024-03-15."*
- *"Register a cash movement of -$1,000 in my savings account."*

---

## CD Pipeline

The repository includes a GitHub Actions workflow (`.github/workflows/cd.yml`) that triggers on every push to `main` (and supports manual dispatch via `workflow_dispatch`):

1. Reads the version number from `<Version>` in `PortfolioTracker.csproj`.
2. Builds in Release mode.
3. Publishes a self-contained single-file Windows executable (`win-x64`).
4. Creates a GitHub Release tagged `v{version}` with the zipped `.exe` attached — releases never expire.

To publish a new version, bump `<Version>` in `PortfolioTracker.csproj` and push to `main`.

See [`.github/workflows/cd.yml`](.github/workflows/cd.yml) for details.
