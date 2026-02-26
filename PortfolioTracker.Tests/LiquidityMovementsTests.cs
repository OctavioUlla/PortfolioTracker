using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace PortfolioTracker.Tests;

[TestFixture]
public class LiquidityMovementsTests : PageTest
{
    private const string BaseUrl = "http://localhost:5285";

    /// <summary>
    /// Creates a liquidity account and returns its ID, for use in movement tests.
    /// </summary>
    private async Task<string?> CreateAccountAndGetIdAsync(string name)
    {
        await Page.GotoAsync($"{BaseUrl}/LiquidityAccounts/Create");
        var token = await Page.Locator("input[name='__RequestVerificationToken']").First.GetAttributeAsync("value");

        var formData = Page.APIRequest.CreateFormData();
        formData.Set("Name", name);
        formData.Set("__RequestVerificationToken", token ?? "");
        await Page.APIRequest.PostAsync($"{BaseUrl}/LiquidityAccounts/Create", new() { Form = formData });

        await Page.GotoAsync($"{BaseUrl}/LiquidityAccounts");
        var row = Page.Locator("tr", new() { HasText = name });
        var editLink = await row.GetByRole(AriaRole.Link).Nth(1).GetAttributeAsync("href");
        // href is /LiquidityAccounts/Edit/123 — extract the id
        return editLink?.Split('/').Last();
    }

    private async Task<IAPIResponse> PostWithTokenAsync(string url, IFormData formData)
    {
        await Page.GotoAsync($"{BaseUrl}/LiquidityMovements/Create");
        var tokenValue = await Page.Locator("input[name='__RequestVerificationToken']").First.GetAttributeAsync("value");

        formData.Set("__RequestVerificationToken", tokenValue ?? "");
        return await Page.APIRequest.PostAsync(url, new() { Form = formData });
    }

    [Test]
    public async Task LiquidityMovementsPage_Loads_Successfully()
    {
        await Page.GotoAsync($"{BaseUrl}/LiquidityMovements");
        await Expect(Page).ToHaveTitleAsync("Cash Movements - Portfolio Tracker");
    }

    [Test]
    public async Task LiquidityMovementsPage_Shows_MovementsTable()
    {
        await Page.GotoAsync($"{BaseUrl}/LiquidityMovements");

        await Expect(Page.GetByText("Cash Movements").First).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Add Movement" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task LiquidityMovementsPage_CreatePage_Loads()
    {
        await Page.GotoAsync($"{BaseUrl}/LiquidityMovements/Create");
        await Expect(Page).ToHaveTitleAsync("Add Cash Movement - Portfolio Tracker");

        await Expect(Page.GetByLabel("Date")).ToBeVisibleAsync();
        await Expect(Page.GetByLabel("Account")).ToBeVisibleAsync();
        await Expect(Page.GetByLabel("Amount ($)")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Save Movement" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task LiquidityMovementsPage_CanCreateMovement()
    {
        var accountId = await CreateAccountAndGetIdAsync("Movements Test Account");
        Assert.That(accountId, Is.Not.Null);

        var formData = Page.APIRequest.CreateFormData();
        formData.Set("Date", "2024-07-01");
        formData.Set("LiquidityAccountId", accountId!);
        formData.Set("Amount", "2500");
        await PostWithTokenAsync($"{BaseUrl}/LiquidityMovements/Create", formData);

        await Page.GotoAsync($"{BaseUrl}/LiquidityMovements");
        await Expect(Page.GetByText("$2,500.00").First).ToBeVisibleAsync();
    }

    [Test, Order(99)]
    public async Task LiquidityMovementsPage_CanDeleteMovement()
    {
        var accountId = await CreateAccountAndGetIdAsync("Delete Movement Account");
        Assert.That(accountId, Is.Not.Null);

        // Create a movement via API
        var formData = Page.APIRequest.CreateFormData();
        formData.Set("Date", "2024-08-01");
        formData.Set("LiquidityAccountId", accountId!);
        formData.Set("Amount", "7777");
        await PostWithTokenAsync($"{BaseUrl}/LiquidityMovements/Create", formData);

        await Page.GotoAsync($"{BaseUrl}/LiquidityMovements");
        await Expect(Page.GetByText("$7,777.00").First).ToBeVisibleAsync();

        // Click delete on that row
        var row = Page.Locator("tr", new() { HasText = "$7,777.00" });
        var deleteHref = await row.GetByRole(AriaRole.Link).Last.GetAttributeAsync("href");
        await Page.GotoAsync($"{BaseUrl}{deleteHref}");

        // Confirm deletion
        await Page.GetByRole(AriaRole.Button, new() { Name = "Yes, Delete" }).ClickAsync();

        await Page.GotoAsync($"{BaseUrl}/LiquidityMovements");
        await Expect(Page.GetByText("$7,777.00")).Not.ToBeVisibleAsync();
    }

    [Test]
    public async Task LiquidityMovementsPage_EditPage_Loads()
    {
        var accountId = await CreateAccountAndGetIdAsync("Edit Movement Account");
        Assert.That(accountId, Is.Not.Null);

        // Create a movement first
        var formData = Page.APIRequest.CreateFormData();
        formData.Set("Date", "2024-09-01");
        formData.Set("LiquidityAccountId", accountId!);
        formData.Set("Amount", "3333");
        await PostWithTokenAsync($"{BaseUrl}/LiquidityMovements/Create", formData);

        await Page.GotoAsync($"{BaseUrl}/LiquidityMovements");

        var row = Page.Locator("tr", new() { HasText = "$3,333.00" });
        var editHref = await row.GetByRole(AriaRole.Link).First.GetAttributeAsync("href");
        await Page.GotoAsync($"{BaseUrl}{editHref}");

        await Expect(Page).ToHaveTitleAsync("Edit Cash Movement - Portfolio Tracker");
    }

    [Test]
    public async Task LiquidityMovementsPage_FilterByAccount()
    {
        var accountId = await CreateAccountAndGetIdAsync("Filter Account Test");
        Assert.That(accountId, Is.Not.Null);

        await Page.GotoAsync($"{BaseUrl}/LiquidityMovements?accountId={accountId}");
        await Expect(Page).ToHaveTitleAsync("Cash Movements - Portfolio Tracker");
    }

    [Test]
    public async Task Sidebar_HasCashMovementsLink()
    {
        await Page.GotoAsync($"{BaseUrl}/");

        var link = Page.GetByRole(AriaRole.Link, new() { Name = "Cash Movements" });
        await Expect(link).ToBeVisibleAsync();

        await link.ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Cash Movements - Portfolio Tracker");
    }
}
