# Changelog

All notable changes to the Enterprise Internal PKI Manager will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Security headers middleware (X-Content-Type-Options, X-Frame-Options, CSP, Referrer-Policy, Permissions-Policy) on Portal and Gateway
- HSTS (HTTP Strict Transport Security) enforcement on Portal and Gateway
- Global exception handler returning consistent ApiError responses without leaking internals
- Rate limiting on Portal API endpoints (configurable, 60 requests/60s per principal)
- Health check endpoints (`/health`) on Portal and Gateway
- Correlation ID middleware (X-Correlation-ID) for distributed request tracing
- PKI audit logging for certificate issuance, revocation, and deployment operations (PKI_AUDIT prefix)
- ProducesResponseType attributes on all API endpoints for complete Swagger documentation
- [Produces("application/json")] on all controllers
- Directory.Build.props with centralized build settings and TreatWarningsAsErrors
- version.json for Nerdbank.GitVersioning (NBGV) SemVer release management
- CHANGELOG.md following Keep a Changelog format
- New unit tests for Portal security pipeline (security headers, HSTS, rate limiting, health checks, correlation IDs)
- New unit tests for Gateway security pipeline (security headers, health checks, correlation IDs)

### Changed
- Portal Program.cs: added UseExceptionHandler, UseHsts, UseRateLimiter, UseRouting, MapHealthChecks
- Gateway Program.cs: added UseExceptionHandler, UseHsts, security headers, correlation IDs, MapHealthChecks
- CaController now accepts ILogger for audit logging
- WindowsDeploymentService: replaced Console.WriteLine with ILogger (structured logging)
- IISBindingService: replaced Console.WriteLine with ILogger (structured logging)
- CertificatesController now accepts ILogger for audit logging
- DeploymentsController audit log prefix changed to PKI_AUDIT for consistency

### Fixed
- Missing HSTS headers — browsers now enforce HTTPS-only on subsequent visits
- Missing security headers — XSS, clickjacking, MIME-sniffing protections added
- Missing rate limiting on Portal — prevents enumeration attacks
- Missing global exception handler — internal errors no longer leak stack traces
- Console.WriteLine in Collector services — logs now captured by structured logging pipeline
- Missing correlation IDs — distributed request tracing now possible
