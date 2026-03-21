using Microsoft.Playwright.NUnit;
using Microsoft.Playwright;
using FluentAssertions;

namespace E2E.Tests;

[TestFixture]
public class DeploymentsPageTests : PageTest
{
    private const string BaseUrl = "http://localhost:5175";

    public override BrowserNewContextOptions ContextOptions()
    {
        return new BrowserNewContextOptions
        {
            BaseURL = BaseUrl,
            IgnoreHTTPSErrors = true
        };
    }

    [Test]
    public async Task DeploymentsPage_ShouldLoadSuccessfully()
    {
        await Page.GotoAsync("/deployments");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.GetByText("Deployment Center")).ToBeVisibleAsync();
    }

    [Test]
    public async Task DeploymentsPage_ShouldDisplayStatusCards()
    {
        await Page.GotoAsync("/deployments");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify the 4 status stat cards
        await Expect(Page.GetByText("Completed").First).ToBeVisibleAsync();
        await Expect(Page.GetByText("In Progress").First).ToBeVisibleAsync();
        await Expect(Page.GetByText("Pending").First).ToBeVisibleAsync();
        await Expect(Page.GetByText("Failed").First).ToBeVisibleAsync();
    }

    [Test]
    public async Task DeploymentsPage_ShouldDisplayJobsTable()
    {
        await Page.GotoAsync("/deployments");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify table headers
        await Expect(Page.GetByText("Target Host").First).ToBeVisibleAsync();
        await Expect(Page.GetByText("Store Location").First).ToBeVisibleAsync();
        await Expect(Page.GetByText("Certificate").First).ToBeVisibleAsync();

        // Verify table has rows
        var rows = Page.Locator("table tbody tr");
        var count = await rows.CountAsync();
        count.Should().BeGreaterThan(0, "Deployment jobs table should have at least one row");
    }

    [Test]
    public async Task DeploymentsPage_RefreshButton_ShouldWork()
    {
        await Page.GotoAsync("/deployments");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var refreshButton = Page.GetByText("Refresh");
        await Expect(refreshButton).ToBeVisibleAsync();
        await refreshButton.ClickAsync();

        // After refresh, the page should still be functional
        await Expect(Page.GetByText("Deployment Center")).ToBeVisibleAsync();
    }

    [Test]
    public async Task DeploymentsPage_ShouldShowJobStatuses()
    {
        await Page.GotoAsync("/deployments");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify deployment job statuses are displayed with badges
        var statusBadges = Page.Locator(".badge");
        var count = await statusBadges.CountAsync();
        count.Should().BeGreaterThan(0, "Should have status badges in the jobs table");
    }
}
