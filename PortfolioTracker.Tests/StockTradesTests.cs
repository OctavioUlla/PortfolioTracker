using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace PortfolioTracker.Tests;

[TestFixture]
public class StockTradesTests : PageTest
{
    private const string BaseUrl = "http://localhost:5285";

    private async Task<IAPIResponse> PostWithTokenAsync(string url, IFormData formData)
    {
        await Page.GotoAsync($"{BaseUrl}/StockTrades/Create");
        var tokenValue = await Page.Locator("input[name='__RequestVerificationToken']").First.GetAttributeAsync("value");

        formData.Set("__RequestVerificationToken", tokenValue ?? "");
        return await Page.APIRequest.PostAsync(url, new() { Form = formData });
    }

    [Test]
    public async Task StockTradesPage_Loads_Successfully()
    {
        await Page.GotoAsync($"{BaseUrl}/StockTrades");
        await Expect(Page).ToHaveTitleAsync("Stock Trades - Portfolio Tracker");
    }

    [Test]
    public async Task StockTradesPage_Shows_TradesTable()
    {
        await Page.GotoAsync($"{BaseUrl}/StockTrades");

        await Expect(Page.GetByText("Stock Trades")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Add Trade" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task StockTradesPage_CreatePage_Loads()
    {
        await Page.GotoAsync($"{BaseUrl}/StockTrades/Create");
        await Expect(Page).ToHaveTitleAsync("Add Stock Trade - Portfolio Tracker");

        await Expect(Page.GetByLabel("Date")).ToBeVisibleAsync();
        await Expect(Page.GetByLabel("Type")).ToBeVisibleAsync();
        await Expect(Page.GetByLabel("Ticker Symbol")).ToBeVisibleAsync();
        await Expect(Page.GetByLabel("Quantity")).ToBeVisibleAsync();
        await Expect(Page.GetByLabel("Price per Share ($)")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Save Trade" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task StockTradesPage_CanCreateTrade()
    {
        var formData = Page.APIRequest.CreateFormData();
        formData.Set("Date", "2024-04-01");
        formData.Set("Type", "0");
        formData.Set("Ticker", "AAPL");
        formData.Set("Quantity", "10");
        formData.Set("Price", "175.50");
        formData.Set("Commission", "0");
        await PostWithTokenAsync($"{BaseUrl}/StockTrades/Create", formData);

        await Page.GotoAsync($"{BaseUrl}/StockTrades");
        await Expect(Page.GetByText("AAPL").First).ToBeVisibleAsync();
    }

    [Test, Order(99)]
    public async Task StockTradesPage_CanDeleteTrade()
    {
        // Create a trade via API
        var formData = Page.APIRequest.CreateFormData();
        formData.Set("Date", "2024-04-10");
        formData.Set("Type", "0");
        formData.Set("Ticker", "MSFT");
        formData.Set("Quantity", "5");
        formData.Set("Price", "400.00");
        formData.Set("Commission", "0");
        await PostWithTokenAsync($"{BaseUrl}/StockTrades/Create", formData);

        await Page.GotoAsync($"{BaseUrl}/StockTrades");
        await Expect(Page.GetByText("MSFT").First).ToBeVisibleAsync();

        // Click delete on the MSFT row
        var row = Page.Locator("tr", new() { HasText = "MSFT" });
        await row.GetByRole(AriaRole.Link).Last.ClickAsync();

        // Confirm deletion
        await Page.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();

        await Page.GotoAsync($"{BaseUrl}/StockTrades");
        await Expect(Page.GetByText("MSFT")).Not.ToBeVisibleAsync();
    }

    [Test]
    public async Task StockTradesPage_EditPage_Loads()
    {
        // Create a trade first
        var formData = Page.APIRequest.CreateFormData();
        formData.Set("Date", "2024-05-15");
        formData.Set("Type", "0");
        formData.Set("Ticker", "GOOGL");
        formData.Set("Quantity", "2");
        formData.Set("Price", "170.00");
        formData.Set("Commission", "0");
        await PostWithTokenAsync($"{BaseUrl}/StockTrades/Create", formData);

        await Page.GotoAsync($"{BaseUrl}/StockTrades");

        var row = Page.Locator("tr", new() { HasText = "GOOGL" });
        await row.GetByRole(AriaRole.Link).First.ClickAsync();

        await Expect(Page).ToHaveTitleAsync("Edit Stock Trade - Portfolio Tracker");
    }

    [Test]
    public async Task StockTradesPage_ShowsBuyBadge()
    {
        // Create a buy trade
        var formData = Page.APIRequest.CreateFormData();
        formData.Set("Date", "2024-06-01");
        formData.Set("Type", "0");
        formData.Set("Ticker", "SPY");
        formData.Set("Quantity", "3");
        formData.Set("Price", "500.00");
        formData.Set("Commission", "0");
        await PostWithTokenAsync($"{BaseUrl}/StockTrades/Create", formData);

        await Page.GotoAsync($"{BaseUrl}/StockTrades");
        await Expect(Page.GetByText("Buy").First).ToBeVisibleAsync();
    }

    [Test]
    public async Task Sidebar_HasStockTradesLink()
    {
        await Page.GotoAsync($"{BaseUrl}/");

        var link = Page.GetByRole(AriaRole.Link, new() { Name = "Stock Trades" });
        await Expect(link).ToBeVisibleAsync();

        await link.ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Stock Trades - Portfolio Tracker");
    }
}
