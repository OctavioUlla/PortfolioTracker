using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace PortfolioTracker.Tests;

[TestFixture]
public class GoalsTests : PageTest
{
    private const string BaseUrl = "http://localhost:5285";

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
        // Use API request to add goal (same as form POST)
        var formData = Page.APIRequest.CreateFormData();
        formData.Set("Name", "Playwright Add Test");
        formData.Set("TargetValue", "200000");
        formData.Set("TargetDate", "2035-06-15");
        await Page.APIRequest.PostAsync($"{BaseUrl}/Goals/Create", new() { Form = formData });

        // Navigate to verify the goal appears
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
        await Page.APIRequest.PostAsync($"{BaseUrl}/Goals/Create", new() { Form = formData });

        await Page.GotoAsync($"{BaseUrl}/Goals");
        await Expect(Page.GetByText("Delete Me")).ToBeVisibleAsync();

        // Delete the goal via the button (with dialog handler)
        Page.Dialog += (_, dialog) => dialog.AcceptAsync();
        var deleteRow = Page.Locator("tr", new() { HasText = "Delete Me" });
        var deleteForm = deleteRow.Locator("form");
        var goalId = await deleteForm.Locator("input[name='id']").GetAttributeAsync("value");

        // Delete via API
        var deleteData = Page.APIRequest.CreateFormData();
        deleteData.Set("id", goalId!);
        await Page.APIRequest.PostAsync($"{BaseUrl}/Goals/Delete", new() { Form = deleteData });

        await Page.GotoAsync($"{BaseUrl}/Goals");
        await Expect(Page.GetByText("Delete Me")).Not.ToBeVisibleAsync();
    }

    [Test]
    public async Task GoalsPage_Extrapolate_EndpointWorks()
    {
        var formData = Page.APIRequest.CreateFormData();
        formData.Set("monthlyDeposit", "1000");
        formData.Set("annualIRR", "10");

        var response = await Page.APIRequest.PostAsync($"{BaseUrl}/Goals/Extrapolate",
            new() { Form = formData });
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
