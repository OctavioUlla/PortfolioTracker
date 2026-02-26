using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace PortfolioTracker.Tests;

[TestFixture]
public class DashboardTests : PageTest
{
    private const string BaseUrl = "http://localhost:5285";

    [Test]
    public async Task DashboardPage_Loads_Successfully()
    {
        await Page.GotoAsync($"{BaseUrl}/");
        await Expect(Page).ToHaveTitleAsync("Dashboard - Portfolio Tracker");
    }

    [Test]
    public async Task DashboardPage_Shows_SummaryCards()
    {
        await Page.GotoAsync($"{BaseUrl}/");

        await Expect(Page.GetByText("Portfolio Value").First).ToBeVisibleAsync();
        await Expect(Page.GetByText("Lifetime IRR")).ToBeVisibleAsync();
        await Expect(Page.GetByText("S&P 500 Virtual Value")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Total Cash")).ToBeVisibleAsync();
    }

    [Test]
    public async Task DashboardPage_Shows_StockHoldingsSection()
    {
        await Page.GotoAsync($"{BaseUrl}/");

        await Expect(Page.GetByText("Stock Holdings")).ToBeVisibleAsync();
    }

    [Test]
    public async Task DashboardPage_Shows_CashAccountsSection()
    {
        await Page.GotoAsync($"{BaseUrl}/");

        await Expect(Page.GetByText("Cash Accounts").First).ToBeVisibleAsync();
    }

    [Test]
    public async Task DashboardPage_Shows_RecentTransactionsSection()
    {
        await Page.GotoAsync($"{BaseUrl}/");

        await Expect(Page.GetByText("Recent Transactions")).ToBeVisibleAsync();
    }

    [Test]
    public async Task DashboardPage_Shows_IRRCalculator()
    {
        await Page.GotoAsync($"{BaseUrl}/");

        await Expect(Page.GetByText("IRR Calculator")).ToBeVisibleAsync();
        await Expect(Page.Locator("#irrPeriod")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Calculate IRR" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task DashboardPage_IRRCalculator_LifetimeCalculation()
    {
        await Page.GotoAsync($"{BaseUrl}/");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Calculate IRR" }).ClickAsync();

        await Expect(Page.Locator("#irrResult")).ToBeVisibleAsync(new() { Timeout = 10000 });
        await Expect(Page.Locator("#irrPeriodLabel")).ToContainTextAsync("Lifetime");
    }

    [Test]
    public async Task DashboardPage_IRRCalculator_YearMode_ShowsYearField()
    {
        await Page.GotoAsync($"{BaseUrl}/");

        await Page.Locator("#irrPeriod").SelectOptionAsync("year");

        await Expect(Page.Locator("#irrYearField")).ToBeVisibleAsync();
        await Expect(Page.Locator("#irrCustomFields")).Not.ToBeVisibleAsync();
    }

    [Test]
    public async Task DashboardPage_IRRCalculator_CustomMode_ShowsDateFields()
    {
        await Page.GotoAsync($"{BaseUrl}/");

        await Page.Locator("#irrPeriod").SelectOptionAsync("custom");

        await Expect(Page.Locator("#irrCustomFields")).ToBeVisibleAsync();
        await Expect(Page.Locator("#irrStart")).ToBeVisibleAsync();
        await Expect(Page.Locator("#irrEnd")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Sidebar_HasDashboardLink()
    {
        await Page.GotoAsync($"{BaseUrl}/Goals");

        var dashLink = Page.GetByRole(AriaRole.Link, new() { Name = "Dashboard" });
        await Expect(dashLink).ToBeVisibleAsync();

        await dashLink.ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Dashboard - Portfolio Tracker");
    }

    [Test]
    public async Task DashboardPage_AddTradeLink_NavigatesToStockTradesCreate()
    {
        await Page.GotoAsync($"{BaseUrl}/");

        var addTradeLink = Page.GetByRole(AriaRole.Link, new() { Name = "Add Trade" });
        await Expect(addTradeLink).ToBeVisibleAsync();
        await addTradeLink.ClickAsync();

        await Expect(Page).ToHaveTitleAsync("Add Stock Trade - Portfolio Tracker");
    }

    [Test]
    public async Task DashboardPage_AddTransactionLink_NavigatesToTransactionsCreate()
    {
        await Page.GotoAsync($"{BaseUrl}/");

        var addLink = Page.GetByRole(AriaRole.Link, new() { Name = "Add", Exact = true });
        await Expect(addLink.First).ToBeVisibleAsync();
        await addLink.First.ClickAsync();

        await Expect(Page).ToHaveTitleAsync("Add Transaction - Portfolio Tracker");
    }
}
