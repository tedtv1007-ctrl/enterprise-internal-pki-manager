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

        var anchor = Page.Locator("a.quick-action-link", new() { HasTextString = "Request New Certificate" });
        await anchor.ClickAsync();
        await Page.WaitForURLAsync("**/certificates");

        await Expect(Page.GetByText("Certificate Inventory")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Navigation_DashboardToDeployments()
    {
        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var anchor = Page.Locator("a.quick-action-link", new() { HasTextString = "Manage Deployments" });
        await anchor.ClickAsync();
        await Page.WaitForURLAsync("**/deployments");

        await Expect(Page.GetByText("Deployment Center")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Navigation_DashboardToAgents()
    {
        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var anchor = Page.Locator("a.quick-action-link", new() { HasTextString = "View Agents" });
        await anchor.ClickAsync();
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

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Dashboard" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task Navigation_FullCycle()
    {
        // Dashboard → Certificates → Deployments → Agents → Dashboard
        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Dashboard" })).ToBeVisibleAsync();

        var certAnchor = Page.Locator("a.quick-action-link", new() { HasTextString = "Request New Certificate" });
        await certAnchor.ClickAsync();
        await Page.WaitForURLAsync("**/certificates");
        await Expect(Page.GetByText("Certificate Inventory")).ToBeVisibleAsync();

        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var deployAnchor = Page.Locator("a.quick-action-link", new() { HasTextString = "Manage Deployments" });
        await deployAnchor.ClickAsync();
        await Page.WaitForURLAsync("**/deployments");
        await Expect(Page.GetByText("Deployment Center")).ToBeVisibleAsync();

        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var agentAnchor = Page.Locator("a.quick-action-link", new() { HasTextString = "View Agents" });
        await agentAnchor.ClickAsync();
        await Page.WaitForURLAsync("**/agents");
        await Expect(Page.GetByText("Agent Management")).ToBeVisibleAsync();

        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Dashboard" })).ToBeVisibleAsync();
    }
}
