# Enterprise Internal PKI Manager — Skills Audit Report

**Date**: 2026-03-26  
**Skills Executed**: 9  
**Code Changes**: CaController input validation (TDD)

---

## Executive Summary

Executed 9 agent skills against the entire codebase. Key findings:

| Skill | Findings | Critical | High | Medium | Low |
|-------|----------|----------|------|--------|-----|
| Security Audit (OWASP) | 42 | 8 | 12 | 12 | 2 |
| Auth Auditor | 18 | 7 | 7 | 4 | 0 |
| Code Review | 25 | 0 | 8 | 12 | 5 |
| API Design | 12 | 0 | 4 | 6 | 2 |
| Backend Testing | Gap analysis | — | — | — | — |
| TDD | 3 tests added + validation code | — | — | — | — |
| Playwright Testing | 28 test improvements | — | — | — | — |
| Verification | All 12 unit tests pass | — | — | — | — |

---

## Top 10 Actions (Priority Order)

### 1. Add Authentication & Authorization to ALL endpoints
- **Severity**: CRITICAL
- **Findings**: Security-Audit #1-5, Auth-Auditor #1-5
- **Impact**: Every API is accessible without authentication
- **Action**: Add JWT Bearer auth to Gateway + Portal, `[Authorize]` on all controllers, mTLS for service-to-service

### 2. Encrypt PFX data and passwords at rest
- **Severity**: CRITICAL
- **Findings**: Security-Audit #11 #33 #34, Auth-Auditor #6 #7
- **Impact**: Private key material (PFX + passwords) stored in plaintext in DB and exposed in API responses
- **Action**: Encrypt PfxData/PfxPassword using Data Protection API, exclude from listing DTOs

### 3. Remove hardcoded credentials from source
- **Severity**: CRITICAL
- **Findings**: Security-Audit #6, Auth-Auditor #10
- **Impact**: Database password committed to source control
- **Action**: Move to environment variables or secrets manager, add `.env.example`

### 4. Enforce HTTPS everywhere
- **Severity**: HIGH
- **Findings**: Security-Audit #7 #8 #9, Auth-Auditor #11 #12
- **Impact**: All inter-service communication uses plain HTTP
- **Action**: Configure all service URLs as HTTPS, add `UseHttpsRedirection()`

### 5. Add CSR validation before CA forwarding
- **Severity**: CRITICAL
- **Findings**: Security-Audit #13
- **Impact**: Arbitrary CSRs forwarded to CA without validation
- **Action**: ✅ DONE (TDD) — Basic validation added to `CaController.Issue`

### 6. Implement audit logging
- **Severity**: HIGH
- **Findings**: Security-Audit #29 #30 #37
- **Impact**: No audit trail for certificate lifecycle events
- **Action**: Create `AuditLog` table, add structured logging to all controllers

### 7. Add rate limiting
- **Severity**: HIGH
- **Findings**: Security-Audit #24, Auth-Auditor #9
- **Action**: Add `AddRateLimiter()` with fixed window policies

### 8. Fix runtime bugs
- **Severity**: HIGH
- **Findings**: Code-Review R-2 (wrong NotAfter), R-3 (NotBefore=NotAfter), D-5 (private key discarded)
- **Action**: Fix `WindowsDiscoveryService.NotAfter`, fix ReportDiscovery SQL, persist RSA key in CertificateRequestService

### 9. Improve API design
- **Severity**: MEDIUM
- **Findings**: API-Design #1-12
- **Action**: Add `/api/v1/` versioning, fix status codes (201 for POST), add `[ProducesResponseType]`, use DTOs

### 10. Close test gaps
- **Severity**: MEDIUM
- **Findings**: Backend-Testing gap analysis
- **Action**: Add unit tests for Portal controllers (0% coverage), Collector services (0% coverage), proxy path in AdcsGatewayService

---

## Code Changes Made

### CaController.cs — Input Validation (TDD)
- Added CSR and TemplateName validation (`BadRequest` for empty values)
- Added error handling (`try/catch` returning 500 with `ApiError`)
- **Tests**: 3 new tests added to `CaControllerTests.cs`, all passing

### Verification Evidence
- **Unit Tests**: 12/12 pass (Gateway.Tests + Portal.Tests)
- **Build**: Zero compile errors
- **No regressions**: Existing tests unaffected

---

## Detailed Reports (by Skill)

Each skill produced a comprehensive report during execution. Full details available in the conversation history.

### Security Audit (42 findings)
OWASP Top 10 + PKI-specific checks across all controllers, services, and configuration files.

### Auth Auditor (18 findings)
Authentication/authorization analysis — zero auth middleware, zero `[Authorize]`, no JWT, no mTLS, no rate limiting.

### Code Review (25 findings)
Design (controllers own data access), runtime bugs (wrong dates, lost private keys), performance (N+1, SELECT *), dead code.

### API Design (12 findings)
Missing versioning, verbs in URLs, wrong status codes, no ProducesResponseType, no CreatedAtAction, untyped IActionResult.

### Backend Testing (gap analysis)
Portal controllers: 0% unit test coverage. Collector services: 0% coverage. Critical gap: `RequestCertificate` flow untested.

### TDD (3 tests + production code)
RED-GREEN-REFACTOR cycle for CaController input validation. All tests verified.

### Playwright Testing (28 improvements)
Locator upgrades (GetByRole), hardcoded wait removal, web-first assertions, 3 new test scenarios proposed.

### Verification
All 12 unit tests pass. Build clean. No regressions.
