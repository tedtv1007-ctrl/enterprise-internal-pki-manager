---
name: backend-testing
description: Backend API testing with unit tests and integration tests
---

# Backend Testing

## When to Use
- Adding new API endpoints
- Modifying business logic
- Fixing bugs in backend services
- Setting up test infrastructure for a new project

## Instructions

### Step 1: Set up the test environment
- Install test libraries (xUnit, Moq, FluentAssertions for .NET)
- Configure test database (in-memory or separate DB)
- Separate environment configuration for tests

### Step 2: Write Unit Tests (business logic)
- Test each service method independently
- Mock external dependencies (DB, HTTP clients)
- Cover happy path, edge cases, and error scenarios
- Use Arrange-Act-Assert pattern

### Step 3: Integration Tests (API endpoints)
- Use `WebApplicationFactory<Program>` for .NET
- Test full request/response cycle
- Verify HTTP status codes, response bodies, headers
- Test with real (test) database when possible

### Step 4: Test Isolation
- Each test must be independently runnable
- No shared mutable state between tests
- Clean up test data after each test
- Use fresh fixtures for each test

## Constraints

### Required Rules (MUST)
- Each test tests ONE behavior
- Test names clearly describe the scenario being tested
- All tests must be deterministic (no flaky tests)
- Tests should run in isolation (no order dependency)
- Mock external services, not your own code

### Prohibited (MUST NOT)
- Must NOT use `Thread.Sleep()` or fixed delays in tests
- Must NOT share mutable state between test methods
- Must NOT test private/internal methods directly
- Must NOT ignore failing tests (no `[Skip]` without reason)

## Best Practices
- Use descriptive test names: `MethodName_Scenario_ExpectedResult`
- Keep tests fast (< 1 second per unit test)
- Use builder/factory patterns for complex test data
- Aim for 80%+ code coverage on business logic
