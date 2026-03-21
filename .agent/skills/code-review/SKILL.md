---
name: code-review
description: Code review checklist for .NET web APIs — covers security, performance, design, and testing
---

# Code Review Skill

Adapted from Sentry engineering practices for .NET/C# PKI systems.

## Review Checklist

### Identifying Problems
- **Runtime errors**: Potential exceptions, null reference, out-of-bounds
- **Performance**: Unbounded operations, N+1 queries, unnecessary allocations
- **Side effects**: Unintended behavioral changes affecting other components
- **Backwards compatibility**: Breaking API changes without migration path
- **Database queries**: Complex Dapper queries with unexpected performance
- **Security vulnerabilities**: Injection, XSS, access control gaps, secrets exposure

### Design Assessment
- Do component interactions make logical sense?
- Does the change align with existing project architecture?
- Are there conflicts with current requirements or goals?

### Test Coverage
- Functional tests for business logic
- Integration tests for component interactions
- E2E tests for critical user paths
- Verify edge cases are covered

### Long-Term Impact
Flag for review when changes involve:
- Database schema modifications
- API contract changes
- New framework or library adoption
- Performance-critical code paths
- Security-sensitive functionality (cert issuance, key management)

## .NET-Specific Review Points

### Dapper/SQL
```csharp
// Check: Parameterized queries always
// Check: Connection disposal (using/await using)
// Check: Transaction usage for multi-step operations
```

### API Controllers
```csharp
// Check: Model validation on all inputs
// Check: Proper HTTP status codes (201 for creation, 404 for not found)
// Check: Consistent error response format
// Check: No sensitive data in error messages
```

### HttpClient
```csharp
// Check: Using IHttpClientFactory, not new HttpClient()
// Check: Timeout configuration
// Check: Error handling for external service calls
```

## Feedback Guidelines
- Be polite and constructive
- Provide actionable suggestions
- Phrase as questions when uncertain: "Have you considered...?"
- Don't block for stylistic preferences
- Goal is risk reduction, not perfect code
