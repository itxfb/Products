# Products

A production-style Products service: a .NET 10 minimal API on PostgreSQL, secured with JWTs issued by Keycloak, consumed by a React SPA, and runnable with one command.

## Run it

Prerequisite: Docker Desktop (or Docker Engine with Compose v2).

```bash
docker compose up --build
```

The first run builds the images and imports the Keycloak realm (about a minute). The API applies EF Core migrations on startup, so the database needs no manual step.

| URL | What |
| --- | --- |
| http://localhost:3000 | React app. Sign in as **demo / demo** |
| http://localhost:5080/health | API readiness, including the database |
| http://localhost:5080/health/live | API liveness |
| http://localhost:8080 | Keycloak. Admin console **admin / admin** |

All credentials are local demo values only.

### Develop without the API and web containers

Requires the .NET 10 SDK and Node 24.

```bash
docker compose up -d postgres keycloak
dotnet run --project src/Products.Api
cd web && npm ci && npm run dev
```

The API listens on http://localhost:5080 (OpenAPI at `/openapi/v1.json` in Development) and the web app on http://localhost:5173. Vite proxies `/api` to the API, so the browser stays same-origin.

## Tests

```bash
dotnet test
cd web && npm ci && npm run lint && npm run build
```

`dotnet test` runs the unit tests and the integration tests. The integration tests start PostgreSQL 18 with Testcontainers, so Docker must be running. They drive the real JwtBearer pipeline with tokens signed by a per-run test key.

## API

| Method | Path | Scope | Responses |
| --- | --- | --- | --- |
| GET | `/health` | anonymous | 200 Healthy, 503 Unhealthy |
| GET | `/health/live` | anonymous | 200 Healthy |
| GET | `/api/products?colour=&after=&limit=` | `products:read` | 200 JSON array, 400, 401, 403 |
| GET | `/api/products/{id}` | `products:read` | 200, 404, 401, 403 |
| POST | `/api/products` | `products:write` | 201 with `Location: /api/products/{id}`, 400, 401, 403 |

`POST` takes `{ "name": "Desk", "colour": "Black", "price": 120 }`. Every error is an RFC 9457 ProblemDetails. `colour` filters case-insensitively; `limit` is 1–100 (default 50); pass the last `id` you received as `after` to fetch the next page.

## Configuration

| Setting | Purpose |
| --- | --- |
| `ConnectionStrings__Products` | PostgreSQL connection string |
| `Authentication__Schemes__Bearer__Authority` | Issuer URL used to discover signing keys (local development) |
| `Authentication__Schemes__Bearer__MetadataAddress` | Discovery document URL reachable from the API (Compose: internal host) |
| `Authentication__Schemes__Bearer__ValidIssuer` | Public issuer URL tokens must carry when discovery runs on another host |
| `Authentication__Schemes__Bearer__ValidAudience` | Required audience, `products-api` by default |
| `Authentication__Schemes__Bearer__RequireHttpsMetadata` | `false` only for local and Compose |
| `VITE_OIDC_AUTHORITY`, `VITE_OIDC_CLIENT_ID` | Web build arguments, default to the local realm |
| `IDP_ORIGIN` | Identity provider origin allowed by the web CSP |

## Assumptions

- Prices share one implicit currency, are stored as `numeric(18,2)` and are rounded half away from zero to two decimals on create.
- Colour is free text. The filter matches the whole value case-insensitively through an ICU collation; it is not a substring search.
- Lists are ordered by `Id` (UUIDv7, so roughly by creation time) because keyset pagination needs a unique, stable order. A page shorter than `limit` is the last one.
- Any user holding the scopes may create products; the demo realm grants both scopes by default.
- Validation error keys are the C# property names (`Name`, `Colour`, `Price`), as produced by .NET 10's built-in validation; the web client matches them case-insensitively.
- On the very first start against an empty database the API logs one failed `SELECT` on `__EFMigrationsHistory`. Npgsql probes the table that way by design, then the migration runs.
- Data Protection logging is limited to errors: the API is a stateless JWT resource server and protects no data with it.

## Trade-offs

- **Startup migrations vs migration bundles.** Migrating on startup gives the one-command run, and EF Core 9+ takes a database lock so replicas starting together cannot race. It does need DDL rights at runtime and ties deployments to schema changes; in production run a `dotnet ef migrations bundle` as a pipeline step or init container with a privileged account and give the app a least-privilege user.
- **Keyset vs offset pagination.** `WHERE Id > @after ORDER BY Id LIMIT n`, served by the primary key and the `(Colour, Id)` index, costs the same on page 1 and page 10,000 and does not skip or repeat rows when products are added. It cannot jump to page N or return a total; offset can, but it scans and discards rows and slows down as the table grows.
- **Keycloak dev mode.** `start-dev` uses an embedded H2 database, plain HTTP and development defaults. Production needs `start` with an external database, TLS, a fixed hostname and managed secrets.
- **Tokens in the browser.** oidc-client-ts keeps tokens in session storage behind a strict CSP. A backend-for-frontend with HttpOnly cookies is stronger for high-risk applications.
- **API container health.** The chiseled runtime image has no shell or HTTP client, so the Compose file has no API `HEALTHCHECK`; orchestrators probe `/health/live` and `/health` directly.
- **Deferred.** Rate limiting, caching, API versioning, OpenTelemetry, messaging with an outbox, update and delete endpoints, seed data, CI and front-end tests are out of scope; the architecture doc shows where they fit.

## Architecture

See [docs/architecture.md](docs/architecture.md).
