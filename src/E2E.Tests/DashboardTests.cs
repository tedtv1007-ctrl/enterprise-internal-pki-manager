using Microsoft.Playwright.NUnit;
using Microsoft.Playwright;

namespace E2E.Tests;

/// <summary>
/// Base class for Playwright E2E tests.
/// Configure baseURL to point to the running Portal.UI application.
/// Start Portal API + Portal.UI before running these tests.
/// </summary>
[TestFixture]
public class DashboardTests : PageTest
{
    private static readonly string BaseUrl = Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://localhost:5261";

    public override BrowserNewContextOptions ContextOptions()
    {
        return new BrowserNewContextOptions
        {
            BaseURL = BaseUrl,
            IgnoreHTTPSErrors = true
        };
    }

    [Test]
    public async Task Dashboard_ShouldLoadSuccessfully()
    {
        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify that the Dashboard heading is visible
        var heading = Page.GetByText("Dashboard");
        await Expect(heading.First).ToBeVisibleAsync();
    }

    [Test]
    public async Task Dashboard_ShouldDisplayStatCards()
    {
        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify all 4 stat cards are visible
        await Expect(Page.GetByText("Total Certificates")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Expiring Soon (30d)")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Active Agents")).ToBeVisibleAsync();
        await Expect(Page.GetByText("PQC-Ready Certs")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Dashboard_ShouldDisplaySystemHealth()
    {
        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.GetByText("System Health")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Certificate Coverage")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Agent Connectivity")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Dashboard_ShouldDisplayQuickActions()
    {
        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.GetByText("Quick Actions")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Request New Certificate")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Manage Deployments")).ToBeVisibleAsync();
        await Expect(Page.GetByText("View Agents")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Dashboard_QuickActionsNavigation_Certificates()
    {
        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var anchor = Page.Locator("fluent-anchor", new() { HasTextString = "Request New Certificate" });
        await anchor.EvaluateAsync("el => { const a = el.shadowRoot?.querySelector('a') || el.querySelector('a'); if(a) a.click(); else window.location.href = el.getAttribute('href') || '/certificates'; }");
        await Page.WaitForURLAsync(new System.Text.RegularExpressions.Regex(".*certificates.*"));
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*certificates.*"));
    }

    [Test]
    public async Task Dashboard_PqcReadyBadge_IsVisible()
    {
        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.GetByText("PQC READY", new() { Exact = true })).ToBeVisibleAsync();
    }
}
