---
name: security-audit
description: Security audit checklist for PKI and web API systems based on OWASP Top 10 and Sentry find-bugs methodology
---

# Security Audit Skill

For auditing PKI Manager code for security vulnerabilities, insecure defaults, and attack surfaces.

## When to Use
- Before deploying to production
- After modifying API endpoints or database queries
- When adding new authentication/authorization logic
- During code review of security-sensitive PKI operations

## Security Audit Phases

### Phase 1: Attack Surface Mapping
For each changed file, identify:
- All user inputs (request params, headers, body, URL components)
- All database queries (especially raw SQL / Dapper)
- All authentication/authorization checks
- All external API calls (Gateway, ADCS proxy)
- All cryptographic operations (certificate handling, key management)

### Phase 2: OWASP Top 10 Checklist
- [ ] **A01: Broken Access Control** — RBAC, authorization checks on all endpoints
- [ ] **A02: Cryptographic Failures** — HTTPS enforced, no hardcoded secrets, proper cert validation
- [ ] **A03: Injection** — Parameterized queries (Dapper `@param`), no string concatenation in SQL
- [ ] **A04: Insecure Design** — Threat modeling for PKI operations
- [ ] **A05: Security Misconfiguration** — No default credentials, CORS restricted, swagger disabled in prod
- [ ] **A06: Vulnerable Components** — `dotnet list package --vulnerable`, NuGet updates
- [ ] **A07: Authentication Failures** — Strong auth on admin APIs, rate limiting
- [ ] **A08: Data Integrity** — Certificate chain validation, CSR verification
- [ ] **A09: Logging Failures** — Security event logging (cert issuance, revocation, deployment)
- [ ] **A10: SSRF** — Validate outbound requests to Gateway/ADCS proxy

### Phase 3: PKI-Specific Checks
- [ ] Private keys never exposed in API responses
- [ ] Certificate raw data (PFX) encrypted at rest
- [ ] PFX passwords not logged or stored in plain text
- [ ] CSR validation before forwarding to CA
- [ ] Certificate expiration alerts and monitoring
- [ ] Revocation checks (CRL/OCSP) implemented
- [ ] Audit trail for all certificate lifecycle events

### Phase 4: .NET-Specific Patterns
```csharp
// ❌ SQL Injection Risk (Dapper)
await conn.QueryAsync($"SELECT * FROM Certificates WHERE Id = '{id}'");

// ✅ Parameterized Query (Dapper)  
await conn.QueryAsync("SELECT * FROM Certificates WHERE Id = @Id", new { Id = id });

// ❌ Secrets in Code
var connString = "Host=prod;Password=secret123";

// ✅ Configuration/Env
var connString = configuration.GetConnectionString("DefaultConnection");

// ❌ No Input Validation
[HttpPost] public async Task<IActionResult> Create(Certificate cert) { ... }

// ✅ With Model Validation
[HttpPost] public async Task<IActionResult> Create([FromBody] CertificateRequest request)
{
    if (!ModelState.IsValid) return BadRequest(ModelState);
    // ...
}
```

## Output Format
For each issue found:
- **File:Line** — Description
- **Severity**: Critical / High / Medium / Low
- **Problem**: What's wrong
- **Fix**: Concrete suggestion
