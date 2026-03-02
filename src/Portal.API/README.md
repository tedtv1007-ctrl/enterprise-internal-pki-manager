# Portal.API (Skeleton)

This directory contains a minimal API skeleton for the Portal service.

Endpoints to implement:
- GET /health -> health check
- GET /certificates -> list certificates (query params: expired, owner)
- POST /certificates/request -> submit CSR (body: csr, metadata)

This is a scaffold intended for development and CI validation. Implementation details and wiring to CA Gateway and Collector are pending.
