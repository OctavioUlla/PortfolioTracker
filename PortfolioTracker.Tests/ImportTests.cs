using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace PortfolioTracker.Tests;

[TestFixture]
public class ImportTests : PageTest
{
    private const string BaseUrl = "http://localhost:5285";

    [Test]
    public async Task ImportPage_Loads_Successfully()
    {
        await Page.GotoAsync($"{BaseUrl}/Import");
        await Expect(Page).ToHaveTitleAsync("Import Excel - Portfolio Tracker");
    }

    [Test]
    public async Task ImportPage_Shows_UploadForm()
    {
        await Page.GotoAsync($"{BaseUrl}/Import");

        await Expect(Page.GetByText("Import Excel Portfolio")).ToBeVisibleAsync();
        await Expect(Page.GetByLabel("Broker")).ToBeVisibleAsync();
        await Expect(Page.GetByLabel("Excel File (.xlsx)")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Upload & Preview" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task ImportPage_Shows_ImportInstructionsTable()
    {
        await Page.GotoAsync($"{BaseUrl}/Import");

        await Expect(Page.GetByText("What will be imported")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Deposit / Withdrawal")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Stock Buy")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Stock Sell")).ToBeVisibleAsync();
    }

    [Test]
    public async Task ImportPage_Shows_CancelButton()
    {
        await Page.GotoAsync($"{BaseUrl}/Import");

        var cancelLink = Page.GetByRole(AriaRole.Link, new() { Name = "Cancel" });
        await Expect(cancelLink).ToBeVisibleAsync();
        await cancelLink.ClickAsync();

        await Expect(Page).ToHaveTitleAsync("Dashboard - Portfolio Tracker");
    }

    [Test]
    public async Task Sidebar_HasImportExcelLink()
    {
        await Page.GotoAsync($"{BaseUrl}/");

        var link = Page.GetByRole(AriaRole.Link, new() { Name = "Import Excel" });
        await Expect(link).ToBeVisibleAsync();

        await link.ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Import Excel - Portfolio Tracker");
    }

    [Test]
    public async Task ImportPage_Shows_FileFormatHint()
    {
        await Page.GotoAsync($"{BaseUrl}/Import");

        await Expect(Page.GetByText("Ahorro e Inversiones.xlsx")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Registro")).ToBeVisibleAsync();
    }
}
