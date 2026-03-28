---
name: enterprise-internal-pki-manager
description: >
  Project-specific guidance for the Enterprise Internal PKI Manager repository.
  Use when working on the .NET PKI platform, including Portal, Gateway,
  Collector, WindowsAgent, Blazor UI, certificate lifecycle automation, ADCS,
  security hardening, API changes, and automated testing.
---

# enterprise-internal-pki-manager

This repository handles internal PKI workflows, certificate issuance, deployment,
and reporting across Portal, Gateway, Collector, and WindowsAgent services.
Treat security, correctness, and verification as first-order concerns.

## When to use

- Working in any project under `src/`
- Changing certificate issuance, storage, deployment, discovery, or audit flows
- Modifying Portal or Gateway APIs
- Modifying Blazor UI in `src/Portal.UI/`
- Adding or changing unit, integration, or E2E tests

## Instructions

1. Start from repository risk. Prioritize authentication, authorization, TLS,
	secret handling, CSR validation, PFX protection, and auditability before
	cosmetic or convenience changes.
2. For backend changes, use this default skill order:
	`security-audit` -> `auth-auditor` -> `api-design` ->
	`backend-testing` -> `test-driven-development` -> `code-review` ->
	`verification-before-completion`.
3. For Blazor UI changes, use this order:
	`playwright-testing` -> `playwright-blazor-testing` ->
	`fluentui-blazor` when the task introduces or maintains Fluent UI Blazor
	components -> `verification-before-completion`.
4. Use TDD for all behavior changes. Write the smallest failing test first,
	confirm the failure for the expected reason, then implement the minimum code
	needed to pass.
5. Match the repository test stacks:
	`Gateway.Tests`, `Portal.Tests`, and `Collector.Tests` use xUnit;
	`E2E.Tests` uses NUnit with Playwright.
6. Prefer targeted verification first, then broaden:
	project-level tests for the touched area, then solution-level build or tests
	when the change crosses boundaries.
7. Do not introduce UI framework churn. `fluentui-blazor` is an opt-in skill for
	work inside `src/Portal.UI/`; do not migrate existing UI to Fluent UI unless
	the task requires it.
8. When touching cross-service flows, verify both API contract and deployment
	path. For example: CSR request -> Gateway -> ADCS/WindowsAgent -> certificate
	persistence -> deployment job -> Collector outcome.
