using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace PortfolioTracker.Tests;

[TestFixture]
public class BrokersTests : PageTest
{
    private const string BaseUrl = "http://localhost:5285";

    private async Task<IAPIResponse> PostWithTokenAsync(string url, IFormData formData)
    {
        await Page.GotoAsync($"{BaseUrl}/Brokers/Create");
        var tokenValue = await Page.Locator("input[name='__RequestVerificationToken']").First.GetAttributeAsync("value");

        formData.Set("__RequestVerificationToken", tokenValue ?? "");
        return await Page.APIRequest.PostAsync(url, new() { Form = formData });
    }

    [Test]
    public async Task BrokersPage_Loads_Successfully()
    {
        await Page.GotoAsync($"{BaseUrl}/Brokers");
        await Expect(Page).ToHaveTitleAsync("Brokers - Portfolio Tracker");
    }

    [Test]
    public async Task BrokersPage_Shows_BrokersTable()
    {
        await Page.GotoAsync($"{BaseUrl}/Brokers");

        await Expect(Page.GetByText("Brokers").First).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Add Broker" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task BrokersPage_CreatePage_Loads()
    {
        await Page.GotoAsync($"{BaseUrl}/Brokers/Create");
        await Expect(Page).ToHaveTitleAsync("Add Broker - Portfolio Tracker");

        await Expect(Page.GetByLabel("Broker Name")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Save Broker" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task BrokersPage_CanCreateBroker()
    {
        var formData = Page.APIRequest.CreateFormData();
        formData.Set("Name", "Playwright Broker Test");
        await PostWithTokenAsync($"{BaseUrl}/Brokers/Create", formData);

        await Page.GotoAsync($"{BaseUrl}/Brokers");
        await Expect(Page.GetByText("Playwright Broker Test")).ToBeVisibleAsync();
    }

    [Test, Order(99)]
    public async Task BrokersPage_CanDeleteBroker()
    {
        // Create a broker via API
        var formData = Page.APIRequest.CreateFormData();
        formData.Set("Name", "Delete Broker Test");
        await PostWithTokenAsync($"{BaseUrl}/Brokers/Create", formData);

        await Page.GotoAsync($"{BaseUrl}/Brokers");
        await Expect(Page.GetByText("Delete Broker Test")).ToBeVisibleAsync();

        // Navigate to delete page
        var row = Page.Locator("tr", new() { HasText = "Delete Broker Test" });
        var deleteHref = await row.GetByRole(AriaRole.Link).Last.GetAttributeAsync("href");
        await Page.GotoAsync($"{BaseUrl}{deleteHref}");

        // Confirm deletion
        await Page.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();

        await Page.GotoAsync($"{BaseUrl}/Brokers");
        await Expect(Page.GetByText("Delete Broker Test")).Not.ToBeVisibleAsync();
    }

    [Test]
    public async Task BrokersPage_EditPage_Loads()
    {
        // Create a broker first
        var formData = Page.APIRequest.CreateFormData();
        formData.Set("Name", "Edit Broker Test");
        await PostWithTokenAsync($"{BaseUrl}/Brokers/Create", formData);

        await Page.GotoAsync($"{BaseUrl}/Brokers");

        var row = Page.Locator("tr", new() { HasText = "Edit Broker Test" });
        var editHref = await row.GetByRole(AriaRole.Link).First.GetAttributeAsync("href");
        await Page.GotoAsync($"{BaseUrl}{editHref}");

        await Expect(Page).ToHaveTitleAsync("Edit Broker - Portfolio Tracker");
        await Expect(Page.GetByLabel("Broker Name")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Sidebar_HasBrokersLink()
    {
        await Page.GotoAsync($"{BaseUrl}/");

        var brokersLink = Page.GetByRole(AriaRole.Link, new() { Name = "Brokers" });
        await Expect(brokersLink).ToBeVisibleAsync();

        await brokersLink.ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Brokers - Portfolio Tracker");
    }
}
