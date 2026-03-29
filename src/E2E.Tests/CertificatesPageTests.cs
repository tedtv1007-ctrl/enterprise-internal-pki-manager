using Microsoft.Playwright.NUnit;
using Microsoft.Playwright;
using FluentAssertions;

namespace E2E.Tests;

[TestFixture]
public class CertificatesPageTests : PageTest
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
    public async Task CertificatesPage_ShouldLoadSuccessfully()
    {
        await Page.GotoAsync("/certificates");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.GetByText("Certificate Inventory")).ToBeVisibleAsync();
    }

    [Test]
    public async Task CertificatesPage_ShouldDisplayTable()
    {
        await Page.GotoAsync("/certificates");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify table headers
        await Expect(Page.GetByText("Common Name").First).ToBeVisibleAsync();
        await Expect(Page.GetByText("Status").First).ToBeVisibleAsync();
        await Expect(Page.GetByText("Algorithm").First).ToBeVisibleAsync();
        await Expect(Page.GetByText("Key Size").First).ToBeVisibleAsync();
        await Expect(Page.GetByText("Expiration").First).ToBeVisibleAsync();
    }

    [Test]
    public async Task CertificatesPage_ShouldDisplayCertificateData()
    {
        await Page.GotoAsync("/certificates");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // The page should load certificates (from API or fallback data)
        // Radzen DataGrid renders standard table rows
        var rows = Page.Locator(".rz-data-grid tbody tr");
        var count = await rows.CountAsync();
        if (count == 0)
        {
            // Fallback: try alternate selector
            rows = Page.Locator("[role='row']").Filter(new() { HasNot = Page.Locator("[role='columnheader']") });
            count = await rows.CountAsync();
        }
        count.Should().BeGreaterThan(0, "Certificate table should have at least one row");
    }

    [Test]
    public async Task CertificatesPage_SearchFilter_ShouldWork()
    {
        await Page.GotoAsync("/certificates");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Type in the search box (Radzen uses standard HTML input)
        var searchInput = Page.GetByPlaceholder("Search CN, Thumbprint...");
        await searchInput.FillAsync("internal");
        await Page.WaitForTimeoutAsync(500); // Debounce wait

        // Results should be filtered
        var visible = Page.GetByText("internal", new() { Exact = false });
        var count = await visible.CountAsync();
        count.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task CertificatesPage_ShouldShowRequestNewCertButton()
    {
        await Page.GotoAsync("/certificates");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.GetByText("Request New Cert")).ToBeVisibleAsync();
    }

    [Test]
    public async Task CertificatesPage_ShouldShowPQCBadge()
    {
        await Page.GotoAsync("/certificates");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // At least one PQC badge should be visible in the fallback data
        var pqcBadge = Page.GetByText("PQC").First;
        await Expect(pqcBadge).ToBeVisibleAsync();
    }
}
