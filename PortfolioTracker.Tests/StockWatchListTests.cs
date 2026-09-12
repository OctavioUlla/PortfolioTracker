using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace PortfolioTracker.Tests;

[TestFixture]
public class StockWatchListTests : PageTest
{
    private const string BaseUrl = "http://localhost:5285";

    /// <summary>
    /// These tests run against the live app and its real SQLite database, so every entry
    /// uses a ticker that no real exchange lists.
    /// </summary>
    private const string TestTicker = "PWTEST";

    private async Task<IAPIResponse> PostWithTokenAsync(string url, IFormData formData)
    {
        await Page.GotoAsync($"{BaseUrl}/StockWatchList");
        var tokenValue = await Page.Locator("input[name='__RequestVerificationToken']").First.GetAttributeAsync("value");

        formData.Set("__RequestVerificationToken", tokenValue ?? "");
        return await Page.APIRequest.PostAsync(url, new() { Form = formData });
    }

    private async Task AddItemAsync(string ticker, string targetPrice)
    {
        var formData = Page.APIRequest.CreateFormData();
        formData.Set("Form.Ticker", ticker);
        formData.Set("Form.Name", "Playwright Test Co.");
        formData.Set("Form.TargetPrice", targetPrice);
        formData.Set("Form.Notes", "Added by the Playwright suite");
        await PostWithTokenAsync($"{BaseUrl}/StockWatchList/Create", formData);
    }

    private async Task RemoveItemAsync(string ticker)
    {
        await Page.GotoAsync($"{BaseUrl}/StockWatchList");
        var row = Page.Locator("tr", new() { HasText = ticker });
        if (await row.CountAsync() == 0) return;

        var id = await row.First.Locator("form[action*='Delete'] input[name='id']").GetAttributeAsync("value");
        var deleteData = Page.APIRequest.CreateFormData();
        deleteData.Set("id", id!);
        await PostWithTokenAsync($"{BaseUrl}/StockWatchList/Delete", deleteData);
    }

    [Test]
    public async Task StockWatchListPage_Loads_Successfully()
    {
        await Page.GotoAsync($"{BaseUrl}/StockWatchList");
        await Expect(Page).ToHaveTitleAsync("Stock Watch List - Portfolio Tracker");
    }

    [Test]
    public async Task StockWatchListPage_Shows_AddForm()
    {
        await Page.GotoAsync($"{BaseUrl}/StockWatchList");

        await Expect(Page.GetByText("Add to Watch List").First).ToBeVisibleAsync();
        await Expect(Page.Locator("input[name='Form.Ticker']")).ToBeVisibleAsync();
        await Expect(Page.Locator("input[name='Form.TargetPrice']")).ToBeVisibleAsync();
    }

    [Test]
    public async Task StockWatchListPage_CanAddItem()
    {
        await RemoveItemAsync(TestTicker);
        await AddItemAsync(TestTicker, "123.45");

        await Page.GotoAsync($"{BaseUrl}/StockWatchList");
        await Expect(Page.GetByText(TestTicker).First).ToBeVisibleAsync();

        var row = Page.Locator("tr", new() { HasText = TestTicker }).First;
        await Expect(row.Locator("input[name='targetPrice']")).ToHaveValueAsync("123.45");

        await RemoveItemAsync(TestTicker);
    }

    [Test]
    public async Task StockWatchListPage_DuplicateTicker_ShowsValidationMessage()
    {
        await RemoveItemAsync(TestTicker);
        await AddItemAsync(TestTicker, "100.00");

        // The same ticker in lower case must be rejected as a duplicate, not stored twice.
        var formData = Page.APIRequest.CreateFormData();
        formData.Set("Form.Ticker", TestTicker.ToLowerInvariant());
        formData.Set("Form.TargetPrice", "90.00");
        var response = await PostWithTokenAsync($"{BaseUrl}/StockWatchList/Create", formData);

        Assert.That(response.Status, Is.EqualTo(200));
        var html = await response.TextAsync();
        Assert.That(html, Does.Contain($"{TestTicker} is already on your watch list."));

        await RemoveItemAsync(TestTicker);
    }

    [Test]
    public async Task StockWatchListPage_CanUpdateTargetPrice()
    {
        await RemoveItemAsync(TestTicker);
        await AddItemAsync(TestTicker, "100.00");

        await Page.GotoAsync($"{BaseUrl}/StockWatchList");
        var row = Page.Locator("tr", new() { HasText = TestTicker }).First;
        var id = await row.Locator("form[action*='UpdateTarget'] input[name='id']").GetAttributeAsync("value");

        var updateData = Page.APIRequest.CreateFormData();
        updateData.Set("id", id!);
        updateData.Set("targetPrice", "77.77");
        await PostWithTokenAsync($"{BaseUrl}/StockWatchList/UpdateTarget", updateData);

        await Page.GotoAsync($"{BaseUrl}/StockWatchList");
        var updatedRow = Page.Locator("tr", new() { HasText = TestTicker }).First;
        await Expect(updatedRow.Locator("input[name='targetPrice']")).ToHaveValueAsync("77.77");

        await RemoveItemAsync(TestTicker);
    }

    [Test, Order(99)]
    public async Task StockWatchListPage_CanDeleteItem()
    {
        await RemoveItemAsync(TestTicker);
        await AddItemAsync(TestTicker, "100.00");

        await Page.GotoAsync($"{BaseUrl}/StockWatchList");
        await Expect(Page.GetByText(TestTicker).First).ToBeVisibleAsync();

        await RemoveItemAsync(TestTicker);

        await Page.GotoAsync($"{BaseUrl}/StockWatchList");
        await Expect(Page.GetByText(TestTicker)).Not.ToBeVisibleAsync();
    }

    [Test]
    public async Task Sidebar_HasStockWatchListLink()
    {
        await Page.GotoAsync($"{BaseUrl}/");

        var link = Page.GetByRole(AriaRole.Link, new() { Name = "Stock Watch List" });
        await Expect(link).ToBeVisibleAsync();

        await link.ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Stock Watch List - Portfolio Tracker");
    }
}
