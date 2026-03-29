using Microsoft.Playwright;

var playwright = await Playwright.CreateAsync();
var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
var page = await browser.NewPageAsync();
await page.GotoAsync("http://localhost:5261/certificates");
await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
await page.WaitForTimeoutAsync(3000);

// Take screenshot before click
await page.ScreenshotAsync(new() { Path = "before-click.png", FullPage = true });

// Try to find the button
var btn = page.GetByText("Request New Cert");
var btnVisible = await btn.IsVisibleAsync();
Console.WriteLine($"Button visible: {btnVisible}");

// Get the button HTML
var btnHtml = await page.Locator("button:has-text('Request New Cert')").EvaluateAsync<string>("el => el.outerHTML");
Console.WriteLine($"Button HTML: {btnHtml}");

// Click the button
await btn.ClickAsync();
await page.WaitForTimeoutAsync(3000);

// Take screenshot after click
await page.ScreenshotAsync(new() { Path = "after-click.png", FullPage = true });

// Check for dialog
var dialogVisible = await page.Locator("[role='dialog']").IsVisibleAsync();
Console.WriteLine($"Dialog visible (role=dialog): {dialogVisible}");

var certDialogVisible = await page.Locator("[data-testid='cert-request-dialog']").IsVisibleAsync();
Console.WriteLine($"Dialog visible (data-testid): {certDialogVisible}");

// Dump page content around dialog area
var bodyHtml = await page.Locator("body").EvaluateAsync<string>("el => el.innerHTML.substring(0, 5000)");
Console.WriteLine($"Body HTML (first 5000 chars):\n{bodyHtml}");

await browser.DisposeAsync();
playwright.Dispose();
