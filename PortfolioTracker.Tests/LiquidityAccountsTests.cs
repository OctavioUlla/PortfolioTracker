using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace PortfolioTracker.Tests;

[TestFixture]
public class LiquidityAccountsTests : PageTest
{
    private const string BaseUrl = "http://localhost:5285";

    private async Task<IAPIResponse> PostWithTokenAsync(string url, IFormData formData)
    {
        await Page.GotoAsync($"{BaseUrl}/LiquidityAccounts/Create");
        var tokenValue = await Page.Locator("input[name='__RequestVerificationToken']").First.GetAttributeAsync("value");

        formData.Set("__RequestVerificationToken", tokenValue ?? "");
        return await Page.APIRequest.PostAsync(url, new() { Form = formData });
    }

    [Test]
    public async Task LiquidityAccountsPage_Loads_Successfully()
    {
        await Page.GotoAsync($"{BaseUrl}/LiquidityAccounts");
        await Expect(Page).ToHaveTitleAsync("Cash Accounts - Portfolio Tracker");
    }

    [Test]
    public async Task LiquidityAccountsPage_Shows_AccountsTable()
    {
        await Page.GotoAsync($"{BaseUrl}/LiquidityAccounts");

        await Expect(Page.GetByText("Cash Accounts").First).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Add Account" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task LiquidityAccountsPage_CreatePage_Loads()
    {
        await Page.GotoAsync($"{BaseUrl}/LiquidityAccounts/Create");
        await Expect(Page).ToHaveTitleAsync("Add Cash Account - Portfolio Tracker");

        await Expect(Page.GetByLabel("Account Name")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Save Account" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task LiquidityAccountsPage_CanCreateAccount()
    {
        var formData = Page.APIRequest.CreateFormData();
        formData.Set("Name", "Playwright Savings Account");
        await PostWithTokenAsync($"{BaseUrl}/LiquidityAccounts/Create", formData);

        await Page.GotoAsync($"{BaseUrl}/LiquidityAccounts");
        await Expect(Page.GetByText("Playwright Savings Account")).ToBeVisibleAsync();
    }

    [Test, Order(99)]
    public async Task LiquidityAccountsPage_CanDeleteAccount()
    {
        // Create an account via API
        var formData = Page.APIRequest.CreateFormData();
        formData.Set("Name", "Delete Account Test");
        await PostWithTokenAsync($"{BaseUrl}/LiquidityAccounts/Create", formData);

        await Page.GotoAsync($"{BaseUrl}/LiquidityAccounts");
        await Expect(Page.GetByText("Delete Account Test")).ToBeVisibleAsync();

        // Click delete on that row
        var row = Page.Locator("tr", new() { HasText = "Delete Account Test" });
        var deleteHref = await row.GetByRole(AriaRole.Link).Last.GetAttributeAsync("href");
        await Page.GotoAsync($"{BaseUrl}{deleteHref}");

        // Confirm deletion
        await Page.GetByRole(AriaRole.Button, new() { Name = "Yes, Delete" }).ClickAsync();

        await Page.GotoAsync($"{BaseUrl}/LiquidityAccounts");
        await Expect(Page.GetByText("Delete Account Test")).Not.ToBeVisibleAsync();
    }

    [Test]
    public async Task LiquidityAccountsPage_EditPage_Loads()
    {
        // Create an account first
        var formData = Page.APIRequest.CreateFormData();
        formData.Set("Name", "Edit Account Test");
        await PostWithTokenAsync($"{BaseUrl}/LiquidityAccounts/Create", formData);

        await Page.GotoAsync($"{BaseUrl}/LiquidityAccounts");

        var row = Page.Locator("tr", new() { HasText = "Edit Account Test" });
        var editHref = await row.GetByRole(AriaRole.Link).Nth(1).GetAttributeAsync("href");
        await Page.GotoAsync($"{BaseUrl}{editHref}");

        await Expect(Page).ToHaveTitleAsync("Edit Cash Account - Portfolio Tracker");
        await Expect(Page.GetByLabel("Account Name")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Sidebar_HasCashAccountsLink()
    {
        await Page.GotoAsync($"{BaseUrl}/");

        var link = Page.GetByRole(AriaRole.Link, new() { Name = "Cash Accounts" });
        await Expect(link).ToBeVisibleAsync();

        await link.ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Cash Accounts - Portfolio Tracker");
    }
}
