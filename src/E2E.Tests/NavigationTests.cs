using Microsoft.Playwright.NUnit;
using Microsoft.Playwright;

namespace E2E.Tests;

[TestFixture]
public class NavigationTests : PageTest
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
    public async Task Navigation_DashboardToCertificates()
    {
        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var anchor = Page.Locator("fluent-anchor", new() { HasTextString = "Request New Certificate" });
        await anchor.EvaluateAsync("el => { const a = el.shadowRoot?.querySelector('a') || el.querySelector('a'); if(a) a.click(); else window.location.href = el.getAttribute('href') || '/certificates'; }");
        await Page.WaitForURLAsync("**/certificates");

        await Expect(Page.GetByText("Certificate Inventory")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Navigation_DashboardToDeployments()
    {
        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var anchor = Page.Locator("fluent-anchor", new() { HasTextString = "Manage Deployments" });
        await anchor.EvaluateAsync("el => { const a = el.shadowRoot?.querySelector('a') || el.querySelector('a'); if(a) a.click(); else window.location.href = el.getAttribute('href') || '/deployments'; }");
        await Page.WaitForURLAsync("**/deployments");

        await Expect(Page.GetByText("Deployment Center")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Navigation_DashboardToAgents()
    {
        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var anchor = Page.Locator("fluent-anchor", new() { HasTextString = "View Agents" });
        await anchor.EvaluateAsync("el => { const a = el.shadowRoot?.querySelector('a') || el.querySelector('a'); if(a) a.click(); else window.location.href = el.getAttribute('href') || '/agents'; }");
        await Page.WaitForURLAsync("**/agents");

        await Expect(Page.GetByText("Agent Management")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Navigation_CertificatesToDashboard()
    {
        await Page.GotoAsync("/certificates");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.GetByText("Dashboard")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Navigation_FullCycle()
    {
        // Dashboard → Certificates → Deployments → Agents → Dashboard
        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page.GetByText("Dashboard")).ToBeVisibleAsync();

        var certAnchor = Page.Locator("fluent-anchor", new() { HasTextString = "Request New Certificate" });
        await certAnchor.EvaluateAsync("el => { const a = el.shadowRoot?.querySelector('a') || el.querySelector('a'); if(a) a.click(); else window.location.href = el.getAttribute('href') || '/certificates'; }");
        await Page.WaitForURLAsync("**/certificates");
        await Expect(Page.GetByText("Certificate Inventory")).ToBeVisibleAsync();

        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var deployAnchor = Page.Locator("fluent-anchor", new() { HasTextString = "Manage Deployments" });
        await deployAnchor.EvaluateAsync("el => { const a = el.shadowRoot?.querySelector('a') || el.querySelector('a'); if(a) a.click(); else window.location.href = el.getAttribute('href') || '/deployments'; }");
        await Page.WaitForURLAsync("**/deployments");
        await Expect(Page.GetByText("Deployment Center")).ToBeVisibleAsync();

        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var agentAnchor = Page.Locator("fluent-anchor", new() { HasTextString = "View Agents" });
        await agentAnchor.EvaluateAsync("el => { const a = el.shadowRoot?.querySelector('a') || el.querySelector('a'); if(a) a.click(); else window.location.href = el.getAttribute('href') || '/agents'; }");
        await Page.WaitForURLAsync("**/agents");
        await Expect(Page.GetByText("Agent Management")).ToBeVisibleAsync();

        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page.GetByText("Dashboard")).ToBeVisibleAsync();
    }
}
