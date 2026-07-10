# Stock Replenishment Request System


Factory workers request material refills when stock runs low at their station,
a reviewer approves or rejects the request, and approved requests get fulfilled.

Built with .NET 10, ASP.NET Core controllers, EF Core (in-memory database),
Blazor + MudBlazor, and NUnit + NSubstitute for testing.

## Getting started

```bash
dotnet build
dotnet run --project StockReplenishment.App
```

Then open http://localhost:5000. The database is seeded on startup with four
users, three locations, and a request in every lifecycle state, so there is
something to look at right away. The ℹ️ icon in the top bar opens a short tour.

Run the tests with:

```bash
dotnet test
```

If you prefer testing the API directly, the `postman/` folder has a ready-made
Postman collection and instructions.

## How to use it

There is no login. Instead,
the **Acting as** dropdown in the top bar switches between the seeded users,
and every API call carries the chosen user in an `X-User-Id` header. The rules
are enforced on the server:

- Only the person who created a request can submit it.
- Approving, rejecting and fulfilling need a reviewer — and never the person
  who created the request, even if they happen to be a reviewer.
- Only reviewers manage stock locations, and a location that still has
  requests attached cannot be deleted.
- A rejection always needs a reason.

The typical flow: act as a worker, create a request (location, priority, one
or more line items), submit it, and watch the stock check resolve on the
detail page. Then switch to a reviewer to approve and fulfill it.

## The slow stock check

The requirement asks for a slow external stock availability check that must not
block the user. My approach:

1. Submitting a request returns **202 Accepted** immediately.
2. The request id goes onto an in-process queue (`Channel<Guid>`).
3. A background worker picks it up, calls the fake external service
   (3–8 seconds, random result), and saves the outcome.
4. The client polls `GET /api/requests/{id}` to learn the result — the detail
   page does this every 2 seconds while the check is pending, so the outcome
   just appears on screen.

## How the code is organized

The folders inside `StockReplenishment.App` mirror the layers from the
assignment's tech-stack table:

- `Domain` — entities and all business rules. The lifecycle lives as methods
  on `ReplenishmentRequest` (`Submit`, `Approve`, `Reject`, `Fulfill`) with
  private setters, so an invalid transition simply cannot happen — from the
  API, the seeder, or anywhere else. These rules are plain C# and are tested
  without any mocks.
- `Services` — application services behind interfaces, plus the background
  worker and the queue. They orchestrate: load, call the domain method, save.
- `BackendApi` — controllers, DTOs, and the exception handler that maps
  domain errors to HTTP problem details (404, 403, 409, 400).
- `DataAccess` / `Database` — the EF Core context and the startup seeder.
  The seeder drives the real domain methods rather than setting statuses
  directly, so seed data can never be in an illegal state.
- `Frontend` — the Blazor pages and layout.

Everything is wired up in `DependencyInjection.cs`, one extension method per
layer. `StockReplenishment.Tests` covers the domain rules, the request
service (in-memory database, mocked queue), and the location service —
33 tests in total.

## Security

The requirement leaves authentication out of scope, but I still wanted the
basics in place:

- **All rules are enforced server-side.** The UI hides buttons you cannot
  use, but that is convenience, not security — every role and lifecycle rule
  is checked again in the domain layer, so the API cannot be talked into an
  illegal action.
- **Input is validated and bounded.** Quantities have ranges, all text fields
  have length limits, and a request is capped at 100 line items, so the API
  does not accept unbounded payloads.
- **Rate limiting.** A per-IP limit (100 requests per 10 seconds) answers
  `429 Too Many Requests` to bursts — enough to blunt accidental or scripted
  hammering without bothering a normal user.
- **Security response headers.** `X-Content-Type-Options: nosniff`,
  `X-Frame-Options: DENY` (no clickjacking) and `Referrer-Policy: no-referrer`
  on every response.
- **Safe by framework.** EF Core parameterizes queries (no SQL injection),
  Blazor HTML-encodes output by default (no XSS from user input), and
  antiforgery protection is enabled.
- **Error responses don't leak internals.** Domain errors map to problem
  details with a business-level message; unexpected exceptions return a
  generic 500 without stack traces.

For production I would add, roughly in this order:

1. **Real authentication and authorization** — the `X-User-Id` header is
   trusted client input and trivially spoofable. Replace it with OpenID
   Connect (e.g. Entra ID or Keycloak) and move role checks into
   authorization policies.
2. **HTTPS everywhere with HSTS**, plus a Content-Security-Policy header.
3. **Audit logging** — who approved or rejected what, and when; this is an
   approval workflow, so traceability is a business requirement too.
4. **Locked-down CORS** if the API ever serves other origins.
5. **Secrets management** (Key Vault / user-secrets) the moment a real
   database or external service credential appears.
6. **Dependency scanning** (Dependabot / `dotnet list package --vulnerable`)
   in CI.
