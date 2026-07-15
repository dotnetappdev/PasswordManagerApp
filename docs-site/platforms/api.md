# API Server

`VaultGuard.API` is an ASP.NET Core Web API - the surface the mobile app and browser extension talk to,
and a fine target for scripts, Postman, or your own integrations. See the full
[API Reference](/api/reference) for endpoints.

## Interactive docs

The API serves a live Swagger UI at `/swagger` when running, generated from the actual controllers -
always in sync with the deployed code.

::: tip
The `Capture Screenshots` GitHub Actions workflow (`.github/workflows/screenshots.yml`) boots the API
and captures this live - it commits a fresh one on every push that touches `VaultGuard.API/**`. The
image below is a placeholder until that first run lands.
:::

<img src="/screenshots/api/swagger.png" alt="Swagger API documentation UI" style="border-radius: 8px; border: 1px solid var(--vp-c-divider)" />

## Health check

```
GET /health
```

Doesn't require an API key - useful for load balancer / uptime checks.

## Rate limiting

A global per-IP limit applies to the whole API, with a stricter limit specifically on authentication
endpoints (`/api/auth/*`) to slow down credential-stuffing attempts.
