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

    /// <summary>
    /// Navigate to the certificates page and wait for Blazor to become interactive.
    /// </summary>
    private async Task NavigateToCertificatesAsync()
    {
        await Page.GotoAsync("/certificates");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        // Wait for Blazor interactive runtime to connect (SignalR/WASM)
        await Page.WaitForFunctionAsync("() => window.Blazor && window.Blazor._internal", new PageWaitForFunctionOptions { Timeout = 15000 });
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
        await NavigateToCertificatesAsync();

        // Wait for data to render after Blazor interactive mode connects
        // Radzen DataGrid renders standard table rows
        var rows = Page.Locator(".rz-data-grid tbody tr");
        await Expect(rows.First).ToBeVisibleAsync(new() { Timeout = 10000 });
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
        await NavigateToCertificatesAsync();

        // Wait for data to appear in the grid first
        await Expect(Page.Locator(".rz-data-grid tbody tr").First).ToBeVisibleAsync(new() { Timeout = 10000 });

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

    [Test]
    public async Task CertificatesPage_RequestNewCert_ShouldOpenDialog()
    {
        await NavigateToCertificatesAsync();

        // Click the "Request New Cert" button
        await Page.GetByTestId("request-new-cert-btn").ClickAsync();

        // Dialog should appear with the title and form fields
        var dialog = Page.GetByTestId("cert-request-dialog");
        await Expect(dialog).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Request New Certificate" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task CertificatesPage_RequestDialog_ShouldShowValidationError()
    {
        await NavigateToCertificatesAsync();

        await Page.GetByTestId("request-new-cert-btn").ClickAsync();
        await Expect(Page.GetByTestId("cert-request-dialog")).ToBeVisibleAsync();

        // Click Submit without filling the form
        await Page.GetByTestId("cert-submit-btn").ClickAsync();

        // Validation error should appear
        var validationError = Page.GetByTestId("cert-validation-error");
        await Expect(validationError).ToBeVisibleAsync();
        var errorText = await validationError.TextContentAsync();
        errorText.Should().Contain("Common Name is required");
    }

    [Test]
    public async Task CertificatesPage_RequestDialog_CancelShouldCloseDialog()
    {
        await NavigateToCertificatesAsync();

        await Page.GetByTestId("request-new-cert-btn").ClickAsync();
        await Expect(Page.GetByTestId("cert-request-dialog")).ToBeVisibleAsync();

        // Click Cancel
        await Page.GetByTestId("cert-cancel-btn").ClickAsync();

        // Dialog should disappear
        await Expect(Page.GetByTestId("cert-request-dialog")).Not.ToBeVisibleAsync();
    }

    [Test]
    public async Task CertificatesPage_RequestDialog_SubmitWithValidData()
    {
        await NavigateToCertificatesAsync();

        await Page.GetByTestId("request-new-cert-btn").ClickAsync();
        await Expect(Page.GetByTestId("cert-request-dialog")).ToBeVisibleAsync();

        // Fill Common Name field
        await Page.GetByPlaceholder("e.g. myapp.internal.enterprise.com").FillAsync("test.internal.enterprise.com");

        // Select algorithm from dropdown
        await Page.GetByTestId("cert-algorithm-select").ClickAsync();
        await Page.GetByText("RSA-4096").ClickAsync();

        // Click Submit
        await Page.GetByTestId("cert-submit-btn").ClickAsync();

        // Dialog should close after successful submission
        await Expect(Page.GetByTestId("cert-request-dialog")).Not.ToBeVisibleAsync();

        // Success notification should appear
        await Expect(Page.GetByText("Certificate Requested")).ToBeVisibleAsync();
    }
}
