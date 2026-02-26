using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace PortfolioTracker.Tests;

[TestFixture]
public class MonthlyBalancesTests : PageTest
{
    private const string BaseUrl = "http://localhost:5285";

    private async Task<IAPIResponse> PostWithTokenAsync(string url, IFormData formData)
    {
        await Page.GotoAsync($"{BaseUrl}/MonthlyBalances/Create");
        var tokenValue = await Page.Locator("input[name='__RequestVerificationToken']").First.GetAttributeAsync("value");

        formData.Set("__RequestVerificationToken", tokenValue ?? "");
        return await Page.APIRequest.PostAsync(url, new() { Form = formData });
    }

    [Test]
    public async Task MonthlyBalancesPage_Loads_Successfully()
    {
        await Page.GotoAsync($"{BaseUrl}/MonthlyBalances");
        await Expect(Page).ToHaveTitleAsync("Monthly Balances - Portfolio Tracker");
    }

    [Test]
    public async Task MonthlyBalancesPage_Shows_BalancesTable()
    {
        await Page.GotoAsync($"{BaseUrl}/MonthlyBalances");

        await Expect(Page.GetByText("Monthly Balances").First).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Add Balance" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task MonthlyBalancesPage_CreatePage_Loads()
    {
        await Page.GotoAsync($"{BaseUrl}/MonthlyBalances/Create");
        await Expect(Page).ToHaveTitleAsync("Add Monthly Balance - Portfolio Tracker");

        await Expect(Page.GetByLabel("Year")).ToBeVisibleAsync();
        await Expect(Page.GetByLabel("Month")).ToBeVisibleAsync();
        await Expect(Page.GetByLabel("Balance ($)")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Save Balance" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task MonthlyBalancesPage_CanCreateBalance()
    {
        var formData = Page.APIRequest.CreateFormData();
        formData.Set("Year", "2024");
        formData.Set("Month", "6");
        formData.Set("Balance", "75000");
        formData.Set("BrokerId", "1");
        await PostWithTokenAsync($"{BaseUrl}/MonthlyBalances/Create", formData);

        await Page.GotoAsync($"{BaseUrl}/MonthlyBalances");
        await Expect(Page.GetByText("$75,000.00").First).ToBeVisibleAsync();
    }

    [Test, Order(99)]
    public async Task MonthlyBalancesPage_CanDeleteBalance()
    {
        // Create a balance via API
        var formData = Page.APIRequest.CreateFormData();
        formData.Set("Year", "2023");
        formData.Set("Month", "12");
        formData.Set("Balance", "99999");
        formData.Set("BrokerId", "1");
        await PostWithTokenAsync($"{BaseUrl}/MonthlyBalances/Create", formData);

        await Page.GotoAsync($"{BaseUrl}/MonthlyBalances");
        await Expect(Page.GetByText("$99,999.00").First).ToBeVisibleAsync();

        // Click delete on the row
        var row = Page.Locator("tr", new() { HasText = "$99,999.00" });
        await row.GetByRole(AriaRole.Link).Last.ClickAsync();

        // Confirm deletion
        await Page.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();

        await Page.GotoAsync($"{BaseUrl}/MonthlyBalances");
        await Expect(Page.GetByText("$99,999.00")).Not.ToBeVisibleAsync();
    }

    [Test]
    public async Task MonthlyBalancesPage_EditPage_Loads()
    {
        // Create a balance first
        var formData = Page.APIRequest.CreateFormData();
        formData.Set("Year", "2024");
        formData.Set("Month", "3");
        formData.Set("Balance", "50000");
        formData.Set("BrokerId", "1");
        await PostWithTokenAsync($"{BaseUrl}/MonthlyBalances/Create", formData);

        await Page.GotoAsync($"{BaseUrl}/MonthlyBalances");

        var row = Page.Locator("tr", new() { HasText = "$50,000.00" });
        await row.GetByRole(AriaRole.Link).First.ClickAsync();

        await Expect(Page).ToHaveTitleAsync("Edit Monthly Balance - Portfolio Tracker");
    }

    [Test]
    public async Task Sidebar_HasMonthlyBalancesLink()
    {
        await Page.GotoAsync($"{BaseUrl}/");

        var link = Page.GetByRole(AriaRole.Link, new() { Name = "Monthly Balances" });
        await Expect(link).ToBeVisibleAsync();

        await link.ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Monthly Balances - Portfolio Tracker");
    }
}
