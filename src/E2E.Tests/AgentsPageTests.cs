using Microsoft.Playwright.NUnit;
using Microsoft.Playwright;
using FluentAssertions;

namespace E2E.Tests;

[TestFixture]
public class AgentsPageTests : PageTest
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
    public async Task AgentsPage_ShouldLoadSuccessfully()
    {
        await Page.GotoAsync("/agents");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.GetByText("Agent Management")).ToBeVisibleAsync();
    }

    [Test]
    public async Task AgentsPage_ShouldDisplayAgentCards()
    {
        await Page.GotoAsync("/agents");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Agent cards should be visible (from API or fallback data)
        // Radzen renders cards as div.rz-card
        var cards = Page.Locator(".rz-card");
        var count = await cards.CountAsync();
        count.Should().BeGreaterThan(0, "Should display agent cards");
    }

    [Test]
    public async Task AgentsPage_ShouldShowOnlineOfflineStatus()
    {
        await Page.GotoAsync("/agents");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Check for ONLINE or OFFLINE badges
        var onlineBadges = Page.GetByText("ONLINE");
        var offlineBadges = Page.GetByText("OFFLINE");

        var onlineCount = await onlineBadges.CountAsync();
        var offlineCount = await offlineBadges.CountAsync();

        (onlineCount + offlineCount).Should().BeGreaterThan(0, "Should show ONLINE or OFFLINE status badges");
    }

    [Test]
    public async Task AgentsPage_ShouldShowAgentCount()
    {
        await Page.GotoAsync("/agents");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // The page header shows "X online / Y total"
        var totalText = Page.GetByText("total");
        await Expect(totalText).ToBeVisibleAsync();
    }

    [Test]
    public async Task AgentsPage_ShouldShowAgentHostnames()
    {
        await Page.GotoAsync("/agents");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify that agent hostnames are displayed (from fallback data)
        // Agent hostnames are rendered inside Radzen cards
        var hostnames = Page.Locator(".rz-card").GetByText("Agent");
        var count = await hostnames.CountAsync();
        count.Should().BeGreaterThan(0, "Should display agent hostnames");
    }

    [Test]
    public async Task AgentsPage_ShouldShowLastHeartbeat()
    {
        await Page.GotoAsync("/agents");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.GetByText("Last Heartbeat").First).ToBeVisibleAsync();
    }
}
