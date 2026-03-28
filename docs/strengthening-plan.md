# Enterprise Internal PKI Manager Strengthening Plan

## Goal

Strengthen the project in an order that reduces PKI risk first, preserves delivery speed, and keeps every production change behind TDD and fresh verification evidence.

## Skill Order

Use local skills in this order by default:

1. `security-audit`
2. `auth-auditor`
3. `api-design`
4. `backend-testing`
5. `test-driven-development`
6. `code-review`
7. `playwright-testing`
8. `playwright-blazor-testing`
9. `fluentui-blazor` only when Fluent UI Blazor is explicitly introduced
10. `verification-before-completion`

## Delivery Strategy

### Phase 1: Data Integrity And Runtime Bugs
- Focus: fix correctness bugs that can corrupt PKI inventory or lifecycle data.
- Skills: `backend-testing` -> `test-driven-development` -> `code-review` -> `verification-before-completion`.
- Tasks:
  - Preserve discovered certificate validity windows end to end.
  - Fix Windows discovery date extraction bugs.
  - Fix incorrect discovery persistence mappings in Portal.
  - Replace deprecated certificate loading paths when behavior-safe.
- Exit criteria:
  - New unit tests exist for every corrected mapper or service path.
  - Touched test projects pass.

### Phase 2: Gateway Security Boundary
- Focus: secure the CA-facing boundary before broader Portal auth rollout.
- Skills: `security-audit` -> `auth-auditor` -> `api-design` -> `backend-testing` -> `test-driven-development` -> `verification-before-completion`.
- Tasks:
  - Add authenticated service-to-service access for Gateway.
  - Add authorization requirements for CA endpoints.
  - Add rate limiting on issuance endpoints.
  - Add focused tests for unauthorized, authorized, and rate-limited requests.
- Exit criteria:
  - Unauthenticated CA requests are rejected.
  - Portal-to-Gateway flow has automated coverage.

### Phase 3: Portal API Hardening
- Focus: protect management APIs without breaking Portal.UI rollout.
- Skills: `security-audit` -> `auth-auditor` -> `api-design` -> `backend-testing` -> `test-driven-development` -> `code-review` -> `verification-before-completion`.
- Tasks:
  - Introduce authentication for Portal API endpoints.
  - Add authorization policy boundaries for admin operations.
  - Remove plaintext secrets from configuration and move to environment or secrets storage.
  - Introduce DTO boundaries to avoid leaking sensitive fields.
- Exit criteria:
  - No management endpoint is anonymously callable.
  - Sensitive PKI fields are excluded from listing responses.

### Phase 4: Certificate Material Protection
- Focus: protect PFX data and lifecycle audit trails.
- Skills: `security-audit` -> `backend-testing` -> `test-driven-development` -> `code-review` -> `verification-before-completion`.
- Tasks:
  - Encrypt PFX data and passwords at rest.
  - Add audit logs for issuance, discovery, deployment, and failure paths.
  - Add tests covering encrypted persistence and audit event emission.
- Exit criteria:
  - PFX material is not stored in plaintext.
  - Critical certificate lifecycle events are auditable.

### Phase 5: UI And E2E Stabilization
- Focus: align Portal.UI and browser tests with secured APIs.
- Skills: `playwright-testing` -> `playwright-blazor-testing` -> `fluentui-blazor` when applicable -> `verification-before-completion`.
- Tasks:
  - Add stable selectors for Blazor components.
  - Remove brittle waits and replace with web-first assertions.
  - Add authenticated UI test setup for protected API flows.
- Exit criteria:
  - Critical UI journeys pass under authenticated conditions.

## TDD Rules

- No production code without a failing test first.
- Keep tests minimal: one behavior per test.
- Verify red for the expected reason before implementation.
- Implement the smallest change to go green.
- Refactor only after green.

## Verification Rules

- Prefer the narrowest test project that proves the change.
- Expand to broader build or test commands only after targeted verification is green.
- Do not claim completion without fresh command output.

## Current Status

- Completed: Phase 1 first tranche.
- Delivered:
  - `CertificateDiscovery.NotBefore` added to the shared discovery model.
  - Collector now preserves real `NotBefore` and `NotAfter` values from X.509 certificates.
  - Portal discovery persistence now maps discovered certificates with the correct validity window.
  - New unit tests cover both mapper paths.
- Next recommended tranche: Phase 2 Gateway security boundary.