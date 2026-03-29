using Microsoft.Playwright.NUnit;
using Microsoft.Playwright;

namespace E2E.Tests;

/// <summary>
/// Captures screenshots of all pages for the user manual.
/// Run with: dotnet test --filter ScreenshotCapture
/// </summary>
[TestFixture]
public class ScreenshotCapture : PageTest
{
    private static readonly string BaseUrl = Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://localhost:5261";
    private static readonly string OutputDir = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "docs", "screenshots"));

    public override BrowserNewContextOptions ContextOptions()
    {
        return new BrowserNewContextOptions
        {
            BaseURL = BaseUrl,
            IgnoreHTTPSErrors = true,
            ViewportSize = new ViewportSize { Width = 1440, Height = 900 }
        };
    }

    [OneTimeSetUp]
    public void EnsureOutputDir()
    {
        Directory.CreateDirectory(OutputDir);
    }

    [Test, Order(1)]
    public async Task Capture_01_Dashboard()
    {
        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForTimeoutAsync(1500);
        await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(OutputDir, "01-dashboard.png"),
            FullPage = true
        });
    }

    [Test, Order(2)]
    public async Task Capture_02_Certificates()
    {
        await Page.GotoAsync("/certificates");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForTimeoutAsync(1500);
        await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(OutputDir, "02-certificates.png"),
            FullPage = true
        });
    }

    [Test, Order(3)]
    public async Task Capture_03_Deployments()
    {
        await Page.GotoAsync("/deployments");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForTimeoutAsync(1500);
        await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(OutputDir, "03-deployments.png"),
            FullPage = true
        });
    }

    [Test, Order(4)]
    public async Task Capture_04_Agents()
    {
        await Page.GotoAsync("/agents");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForTimeoutAsync(1500);
        await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(OutputDir, "04-agents.png"),
            FullPage = true
        });
    }
}
