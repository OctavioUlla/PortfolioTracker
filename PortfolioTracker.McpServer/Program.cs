using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PortfolioTracker.Data;

// Resolve the database path from environment variable or fall back to the current directory.
// Set PORTFOLIO_DB_PATH to the full path of your portfolio.db file when configuring Claude Desktop.
var dbPath = Environment.GetEnvironmentVariable("PORTFOLIO_DB_PATH")
    ?? Path.Combine(Directory.GetCurrentDirectory(), "portfolio.db");

var builder = Host.CreateApplicationBuilder(args);

// Route all logs to stderr so MCP hosts don't confuse them with JSON-RPC messages on stdout.
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
