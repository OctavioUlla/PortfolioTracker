using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace PortfolioTracker.Tests;

[TestFixture]
public class TransactionsTests : PageTest
{
    private const string BaseUrl = "http://localhost:5285";

    private async Task<IAPIResponse> PostWithTokenAsync(string url, IFormData formData)
    {
        await Page.GotoAsync($"{BaseUrl}/Transactions/Create");
        var tokenValue = await Page.Locator("input[name='__RequestVerificationToken']").First.GetAttributeAsync("value");

        formData.Set("__RequestVerificationToken", tokenValue ?? "");
        return await Page.APIRequest.PostAsync(url, new() { Form = formData });
    }

    [Test]
    public async Task TransactionsPage_Loads_Successfully()
    {
        await Page.GotoAsync($"{BaseUrl}/Transactions");
        await Expect(Page).ToHaveTitleAsync("Deposits & Withdrawals - Portfolio Tracker");
    }

    [Test]
    public async Task TransactionsPage_Shows_TransactionsTable()
    {
        await Page.GotoAsync($"{BaseUrl}/Transactions");

        await Expect(Page.GetByText("Cash Transactions")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Add Transaction" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task TransactionsPage_CreatePage_Loads()
    {
        await Page.GotoAsync($"{BaseUrl}/Transactions/Create");
        await Expect(Page).ToHaveTitleAsync("Add Transaction - Portfolio Tracker");

        await Expect(Page.GetByLabel("Date")).ToBeVisibleAsync();
        await Expect(Page.GetByLabel("Type")).ToBeVisibleAsync();
        await Expect(Page.GetByLabel("Amount ($)")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Save Transaction" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task TransactionsPage_CanCreateTransaction()
    {
        var formData = Page.APIRequest.CreateFormData();
        formData.Set("Date", "2024-06-01");
        formData.Set("Type", "0");
        formData.Set("Amount", "3000");
        formData.Set("SP500Price", "5000");
        formData.Set("BrokerId", "1");
        await PostWithTokenAsync($"{BaseUrl}/Transactions/Create", formData);

        await Page.GotoAsync($"{BaseUrl}/Transactions");
        await Expect(Page.GetByText("$3,000.00").First).ToBeVisibleAsync();
    }

    [Test, Order(99)]
    public async Task TransactionsPage_CanDeleteTransaction()
    {
        // Create a transaction via API
        var formData = Page.APIRequest.CreateFormData();
        formData.Set("Date", "2024-01-15");
        formData.Set("Type", "0");
        formData.Set("Amount", "9999");
        formData.Set("SP500Price", "4800");
        formData.Set("Notes", "Playwright Delete Test");
        formData.Set("BrokerId", "1");
        await PostWithTokenAsync($"{BaseUrl}/Transactions/Create", formData);

        await Page.GotoAsync($"{BaseUrl}/Transactions");
        await Expect(Page.GetByText("$9,999.00").First).ToBeVisibleAsync();

        // Click delete on that row
        var row = Page.Locator("tr", new() { HasText = "$9,999.00" });
        var deleteHref = await row.GetByRole(AriaRole.Link).Last.GetAttributeAsync("href");
        await Page.GotoAsync($"{BaseUrl}{deleteHref}");

        // Confirm deletion
        await Page.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();

        await Page.GotoAsync($"{BaseUrl}/Transactions");
        await Expect(Page.GetByText("$9,999.00")).Not.ToBeVisibleAsync();
    }

    [Test]
    public async Task TransactionsPage_EditPage_Loads()
    {
        // Create a transaction first
        var formData = Page.APIRequest.CreateFormData();
        formData.Set("Date", "2024-03-01");
        formData.Set("Type", "0");
        formData.Set("Amount", "1234");
        formData.Set("SP500Price", "5100");
        formData.Set("BrokerId", "1");
        await PostWithTokenAsync($"{BaseUrl}/Transactions/Create", formData);

        await Page.GotoAsync($"{BaseUrl}/Transactions");

        var row = Page.Locator("tr", new() { HasText = "$1,234.00" });
        var editHref = await row.GetByRole(AriaRole.Link).First.GetAttributeAsync("href");
        await Page.GotoAsync($"{BaseUrl}{editHref}");

        await Expect(Page).ToHaveTitleAsync("Edit Transaction - Portfolio Tracker");
    }

    [Test]
    public async Task TransactionsPage_ShowsDepositBadge()
    {
        // Create a deposit
        var formData = Page.APIRequest.CreateFormData();
        formData.Set("Date", "2024-05-01");
        formData.Set("Type", "0");
        formData.Set("Amount", "500");
        formData.Set("SP500Price", "5200");
        formData.Set("BrokerId", "1");
        await PostWithTokenAsync($"{BaseUrl}/Transactions/Create", formData);

        await Page.GotoAsync($"{BaseUrl}/Transactions");
        await Expect(Page.GetByText("Deposit").First).ToBeVisibleAsync();
    }

    [Test]
    public async Task Sidebar_HasTransactionsLink()
    {
        await Page.GotoAsync($"{BaseUrl}/");

        var link = Page.GetByRole(AriaRole.Link, new() { Name = "Deposits & Withdrawals" });
        await Expect(link).ToBeVisibleAsync();

        await link.ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Deposits & Withdrawals - Portfolio Tracker");
    }
}
