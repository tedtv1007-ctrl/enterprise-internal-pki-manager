using Microsoft.Playwright.NUnit;
using Microsoft.Playwright;

namespace E2E.Tests;

[TestFixture]
public class NavigationTests : PageTest
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
    public async Task Navigation_DashboardToCertificates()
    {
        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Click on the Certificates navigation link
        await Page.GetByRole(AriaRole.Link, new() { Name = "Certificates" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.GetByText("Certificate Inventory")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Navigation_DashboardToDeployments()
    {
        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.GetByRole(AriaRole.Link, new() { Name = "Deployments" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.GetByText("Deployment Center")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Navigation_DashboardToAgents()
    {
        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.GetByRole(AriaRole.Link, new() { Name = "Agents" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.GetByText("Agent Management")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Navigation_CertificatesToDashboard()
    {
        await Page.GotoAsync("/certificates");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.GetByRole(AriaRole.Link, new() { Name = "Dashboard" }).ClickAsync();
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

        await Page.GetByRole(AriaRole.Link, new() { Name = "Certificates" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page.GetByText("Certificate Inventory")).ToBeVisibleAsync();

        await Page.GetByRole(AriaRole.Link, new() { Name = "Deployments" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page.GetByText("Deployment Center")).ToBeVisibleAsync();

        await Page.GetByRole(AriaRole.Link, new() { Name = "Agents" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page.GetByText("Agent Management")).ToBeVisibleAsync();

        await Page.GetByRole(AriaRole.Link, new() { Name = "Dashboard" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page.GetByText("Dashboard")).ToBeVisibleAsync();
    }
}
