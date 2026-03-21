---
name: api-design
description: REST API design principles — resource naming, error handling, versioning, pagination
---

# API Design Principles

## Core Principles
1. **Resources are nouns** — `/api/certificates`, `/api/agents`, not `/api/getCertificates`
2. **HTTP methods for actions** — GET (read), POST (create), PUT (replace), PATCH (update), DELETE (remove)
3. **Consistent naming** — plural nouns, kebab-case, hierarchical URLs
4. **Proper status codes** — 200 OK, 201 Created, 204 No Content, 400 Bad Request, 404 Not Found, 409 Conflict, 500 Internal Error

## Error Handling
```csharp
// Consistent error response format
public record ApiError(string Error, string Message, object? Details = null);

// 404 — Not Found
return NotFound(new ApiError("NotFound", $"Certificate {id} not found"));

// 400 — Bad Request (validation)
return BadRequest(new ApiError("ValidationError", "Invalid certificate request", ModelState));

// 409 — Conflict
return Conflict(new ApiError("Conflict", "Certificate with this thumbprint already exists"));
```

## Pagination
```csharp
// GET /api/certificates?page=1&pageSize=20
[HttpGet]
public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
{
    var total = await GetCount();
    var items = await GetPage(page, pageSize);
    return Ok(new { Items = items, Page = page, PageSize = pageSize, TotalCount = total });
}
```

## Versioning Strategy
- URL versioning: `/api/v1/certificates` (recommended for simplicity)
- Breaking changes → new version
- Non-breaking additions → same version

## Best Practices
- Use `[ProducesResponseType]` attributes for Swagger documentation
- Return `CreatedAtAction()` for POST endpoints with Location header
- Use `ActionResult<T>` for type-safe responses
- Implement `IActionFilter` for cross-cutting concerns (logging, validation)
- Always validate model state
