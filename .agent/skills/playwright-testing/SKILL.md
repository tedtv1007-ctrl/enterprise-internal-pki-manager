---
name: playwright-testing
description: Browser automation and E2E testing with Playwright - 70+ production-tested patterns
---

# Playwright E2E Testing Skill

## Golden Rules
1. **`getByRole()` over CSS/XPath** — resilient to markup changes, mirrors how users see the page
2. **Never hardcode waits** — use `expect(locator).toBeVisible()` or `page.WaitForURLAsync()`
3. **Web-first assertions** — auto-retry assertions that wait for elements
4. **Isolate every test** — no shared state, no execution-order dependencies
5. **`baseURL` in config** — zero hardcoded URLs in tests
6. **Retries: `2` in CI, `0` locally** — surface flakiness where it matters
7. **Traces: `'on-first-retry'`** — rich debugging artifacts without CI slowdown
8. **One behavior per test** — multiple related assertions are fine
9. **Mock external services only** — never mock your own app; mock third-party APIs

## .NET Playwright Setup

```csharp
// Install NuGet packages:
// Microsoft.Playwright
// Microsoft.Playwright.NUnit (or xUnit integration)

// Install browsers:
// pwsh bin/Debug/net10.0/playwright.ps1 install
```

## Test Structure

```csharp
[TestFixture]
public class CertificatePageTests : PageTest
{
    [Test]
    public async Task ShouldDisplayCertificateList()
    {
        await Page.GotoAsync("/certificates");
        
        // Use role-based selectors
        var heading = Page.GetByRole(AriaRole.Heading, new() { Name = "Certificates" });
        await Expect(heading).ToBeVisibleAsync();
        
        // Wait for data to load
        var table = Page.GetByRole(AriaRole.Table);
        await Expect(table).ToBeVisibleAsync();
    }
}
```

## Locator Strategy (Priority)
1. `GetByRole()` — buttons, headings, links, tables
2. `GetByText()` — visible text content
3. `GetByLabel()` — form inputs
4. `GetByPlaceholder()` — input placeholders
5. `GetByTestId()` — data-testid attributes (last resort)
6. CSS selector — only when above options fail

## Assertions
```csharp
// ✅ Auto-retrying (web-first)
await Expect(Page.GetByRole(AriaRole.Button)).ToBeVisibleAsync();
await Expect(Page.GetByText("Success")).ToBeVisibleAsync();

// ❌ Non-retrying (avoid)
var text = await Page.TextContentAsync(".status");
Assert.Equal("Success", text);
```

## Network Mocking
```csharp
// Mock API responses for isolated testing
await Page.RouteAsync("**/api/certificates", async route =>
{
    await route.FulfillAsync(new()
    {
        ContentType = "application/json",
        Body = "[{\"id\":\"...\",\"commonName\":\"test.com\"}]"
    });
});
```

## Best Practices
- Start servers before tests, stop after
- Use `page.WaitForLoadStateAsync("networkidle")` for dynamic content
- Take screenshots on failure for debugging
- Use Page Object Model for complex pages
- Test user flows, not implementation details
