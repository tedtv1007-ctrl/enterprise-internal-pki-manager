# Enterprise Internal PKI Manager Instructions

This repository is a .NET 10 PKI platform with these major areas:
- `src/Portal/`: orchestration API and business workflows
- `src/Gateway/`: CA-facing API and ADCS integration boundary
- `src/Collector/`: discovery and deployment agent logic
- `src/WindowsAgent/`: Windows-specific ADCS/DCOM proxy behavior
- `src/Portal.UI/`: Blazor UI

## Priority Order

When strengthening this project, use this skill and workflow order by default:

1. `security-audit`
2. `auth-auditor`
3. `api-design`
4. `backend-testing`
5. `test-driven-development`
6. `code-review`
7. `playwright-testing` for existing browser tests
8. `playwright-blazor-testing` for Blazor-specific E2E patterns
9. `fluentui-blazor` only when work explicitly uses or introduces Fluent UI Blazor
10. `verification-before-completion`

## TDD Rules

- No production code without a failing test first.
- For backend changes, add or update the smallest failing xUnit test in the nearest test project before implementation.
- For UI behavior changes, add or update the smallest failing Playwright/NUnit test before implementation when the behavior is user-visible and stable to automate.
- Confirm the red state for the expected reason, then write the minimum implementation to go green.
- Refactor only after green.

## Test Project Mapping

- `src/Gateway.Tests/`: Gateway unit tests with xUnit
- `src/Portal.Tests/`: Portal unit tests with xUnit
- `src/Collector.Tests/`: Collector unit tests with xUnit
- `src/Integration.Tests/`: cross-service integration tests
- `src/E2E.Tests/`: browser tests with NUnit + Playwright

## Verification Order

Use the narrowest command that proves the change, then expand if the change crosses boundaries.

- `dotnet test src/Gateway.Tests/Gateway.Tests.csproj`
- `dotnet test src/Portal.Tests/Portal.Tests.csproj`
- `dotnet test src/Collector.Tests/Collector.Tests.csproj`
- `dotnet test src/Integration.Tests/Integration.Tests.csproj`
- `dotnet test src/E2E.Tests/E2E.Tests.csproj`
- `dotnet build src/EnterprisePKI.sln`
- `dotnet test src/EnterprisePKI.sln`

## Security and Architecture Bias

- Prefer fixes that reduce PKI risk at the root: authn/authz, TLS, secrets, input validation, audit logs, encrypted key material, and explicit API contracts.
- Treat plaintext PFX data, weak transport, missing `[Authorize]`, and missing rate limiting as priority defects.
- Keep Gateway and Windows-specific behavior isolated from Portal and UI concerns.
- Avoid broad refactors unless they directly improve security, correctness, testability, or a user-requested outcome.

## UI Guidance

- The current UI is Blazor. Use `playwright-blazor-testing` for selectors, waits, and Blazor-specific navigation behavior.
- Use `fluentui-blazor` only if a task explicitly adopts `Microsoft.FluentUI.AspNetCore.Components`.
- Do not add Fluent UI scripts or styles manually; follow the skill guidance if Fluent UI is introduced.

## Completion Standard

- Do not claim success without fresh verification evidence.
- When tests are added or changed, report exactly which test command passed.
- If a broader test suite cannot be run, state that clearly and give the narrowest verified scope.
