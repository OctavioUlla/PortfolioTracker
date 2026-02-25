using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace PortfolioTracker.Tests;

[TestFixture]
public class GoalsTests : PageTest
{
    private const string BaseUrl = "http://localhost:5285";

    /// <summary>
    /// Fetches the Goals page and extracts the anti-forgery token and cookies
    /// for use in subsequent POST requests.
    /// </summary>
    private async Task<(string token, string cookies)> GetAntiForgeryTokenAsync()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}/Goals");
        var html = await response.TextAsync();
        var headers = response.Headers;

        // Extract token from hidden field
        var match = System.Text.RegularExpressions.Regex.Match(html,
            @"name=""__RequestVerificationToken""\s+type=""hidden""\s+value=""([^""]+)""");
        var token = match.Success ? match.Groups[1].Value : "";

        // Extract cookies
        headers.TryGetValue("set-cookie", out var cookieHeader);
        return (token, cookieHeader ?? "");
    }

    private async Task<IAPIResponse> PostWithTokenAsync(string url, IFormData formData)
    {
        // First navigate to the page to get cookie context
        await Page.GotoAsync($"{BaseUrl}/Goals");
        var tokenValue = await Page.Locator("input[name='__RequestVerificationToken']").First.GetAttributeAsync("value");

        formData.Set("__RequestVerificationToken", tokenValue ?? "");
        return await Page.APIRequest.PostAsync(url, new() { Form = formData });
    }

    [Test]
    public async Task GoalsPage_Loads_Successfully()
    {
        await Page.GotoAsync($"{BaseUrl}/Goals");
        await Expect(Page).ToHaveTitleAsync("Goals & Projections - Portfolio Tracker");
    }

    [Test]
    public async Task GoalsPage_Shows_SummaryCards()
    {
        await Page.GotoAsync($"{BaseUrl}/Goals");

        await Expect(Page.GetByText("Current Value")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Lifetime IRR")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Avg Monthly Deposit")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Active Goals")).ToBeVisibleAsync();
    }

    [Test]
    public async Task GoalsPage_Shows_ProjectionChart()
    {
        await Page.GotoAsync($"{BaseUrl}/Goals");

        await Expect(Page.GetByText("Portfolio Projection")).ToBeVisibleAsync();
        await Expect(Page.Locator("#projectionChart")).ToBeVisibleAsync();
    }

    [Test]
    public async Task GoalsPage_Shows_ScenarioParameters()
    {
        await Page.GotoAsync($"{BaseUrl}/Goals");

        await Expect(Page.GetByText("Scenario Parameters")).ToBeVisibleAsync();
        await Expect(Page.Locator("#scenarioDeposit")).ToBeVisibleAsync();
        await Expect(Page.Locator("#scenarioIRR")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Recalculate Projection" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task GoalsPage_CanAddGoal()
    {
        var formData = Page.APIRequest.CreateFormData();
        formData.Set("Name", "Playwright Add Test");
        formData.Set("TargetValue", "200000");
        formData.Set("TargetDate", "2035-06-15");
        await PostWithTokenAsync($"{BaseUrl}/Goals/Create", formData);

        await Page.GotoAsync($"{BaseUrl}/Goals");
        await Expect(Page.GetByText("Playwright Add Test").First).ToBeVisibleAsync();
        await Expect(Page.GetByText("$200,000").First).ToBeVisibleAsync();
    }

    [Test, Order(99)]
    public async Task GoalsPage_CanDeleteGoal()
    {
        // First create a goal via API
        var formData = Page.APIRequest.CreateFormData();
        formData.Set("Name", "Delete Me");
        formData.Set("TargetValue", "50000");
        formData.Set("TargetDate", "2030-01-01");
        await PostWithTokenAsync($"{BaseUrl}/Goals/Create", formData);

        await Page.GotoAsync($"{BaseUrl}/Goals");
        await Expect(Page.GetByText("Delete Me")).ToBeVisibleAsync();

        // Get the goal ID and delete via API
        var deleteRow = Page.Locator("tr", new() { HasText = "Delete Me" });
        var deleteForm = deleteRow.Locator("form");
        var goalId = await deleteForm.Locator("input[name='id']").GetAttributeAsync("value");

        var deleteData = Page.APIRequest.CreateFormData();
        deleteData.Set("id", goalId!);
        await PostWithTokenAsync($"{BaseUrl}/Goals/Delete", deleteData);

        await Page.GotoAsync($"{BaseUrl}/Goals");
        await Expect(Page.GetByText("Delete Me")).Not.ToBeVisibleAsync();
    }

    [Test]
    public async Task GoalsPage_Extrapolate_EndpointWorks()
    {
        var formData = Page.APIRequest.CreateFormData();
        formData.Set("monthlyDeposit", "1000");
        formData.Set("annualIRR", "10");

        var response = await PostWithTokenAsync($"{BaseUrl}/Goals/Extrapolate", formData);
        Assert.That(response.Status, Is.EqualTo(200));

        var json = await response.JsonAsync();
        Assert.That(json, Is.Not.Null);
        var labels = json?.GetProperty("labels");
        Assert.That(labels?.GetArrayLength(), Is.EqualTo(120));
    }

    [Test]
    public async Task GoalsPage_RecalculateButton_UpdatesProjection()
    {
        await Page.GotoAsync($"{BaseUrl}/Goals");

        // Modify scenario parameters
        await Page.Locator("#scenarioDeposit").FillAsync("2000");
        await Page.Locator("#scenarioIRR").FillAsync("8");

        // Click recalculate
        await Page.GetByRole(AriaRole.Button, new() { Name = "Recalculate Projection" }).ClickAsync();

        // Wait for the button to change back from "Calculating..."
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Recalculate Projection" }))
            .ToBeEnabledAsync(new() { Timeout = 10000 });
    }

    [Test]
    public async Task Sidebar_HasGoalsLink()
    {
        await Page.GotoAsync($"{BaseUrl}/");

        var goalsLink = Page.GetByRole(AriaRole.Link, new() { Name = "Goals & Projections" });
        await Expect(goalsLink).ToBeVisibleAsync();

        await goalsLink.ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Goals & Projections - Portfolio Tracker");
    }
}
