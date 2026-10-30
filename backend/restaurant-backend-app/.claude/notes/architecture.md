# Architecture Notes

Short reference for how the backend is built. See `CLAUDE.md` for the entry point.

## Components

| Component | Trigger | Purpose |
|---|---|---|
| `api-handler` | API Gateway (proxy, all routes) | All REST endpoints: auth, users, locations, tables, waiters, dishes, reservations, orders, guest/group flows, feedback, notifications, reports request |
| `reports-handler` | SQS (`reports_queue_name`) | Worker that generates report data for a requested report |
| `report-sender` | SQS (`reports_queue_name`) | Builds report files and sends them to the user via SES |

Shared AWS resources (defined in `deployment_resources.json`, referenced via `${...}` placeholders in `lambda_config.json`):
- DynamoDB tables: Locations, Dishes, Reservations, Orders, Tables, WaiterList, Feedback, Notifications, Reports, ReportsHistory, ProfileImages, GroupReservations, GuestSessions
- Cognito user pool (roles as groups; `AdminAddUserToGroup`, etc.)
- S3 buckets: images, profile images, Swagger UI hosting, deployment artifacts
- SQS queue for reports
- API Gateway with `develop` stage; CORS enabled per resource; routes use `authorization_type: NONE` in Syndicate, auth is checked inside the Lambda (Cognito ID token)

## Request lifecycle (api-handler)

1. `Function` constructor builds the route table once per container (cold start): `RouteRegistry.GetAllRoutes()` groups routes by HTTP method.
2. `FunctionHandler` answers `OPTIONS` (CORS) directly, then matches method + path.
3. The matched `RouteHandler` extracts path parameters into `request.PathParameters`.
4. Handler runs; any exception goes to `ExceptionHandler.Handle`, which maps domain exceptions to HTTP status codes and logs via `context.Logger`.
5. Unknown route → `404 Not Found`.

Note: route matching is first-match over the list for that method, so order of registration matters when paths overlap (e.g. `/dishes/popular` is registered before `/dishes/{id}`).

## Dependency injection

- Built with `Microsoft.Extensions.DependencyInjection`.
- AWS clients (DynamoDB, DynamoDB DataModel context, Cognito, SQS, S3) are singletons created once.
- Repositories → Services → Handlers are created with `ActivatorUtilities.CreateInstance<T>(provider)`, so handler constructors receive services by type.

## Auth model

- Users sign in through `/signin`; Cognito returns `idToken` and `accessToken`.
- `CognitoIdTokenValidator` validates the token (Microsoft.IdentityModel).
- Roles come from Cognito groups; `RoleHelper` checks them per endpoint.
- Guests without accounts use invite tokens (`/guest/invites/{token}`) and guest session IDs; `GuestAuthorizationService` reads `Authorization: Bearer ...`, hashes it with SHA-256 and matches `guestSessionTokenHash` (GSI on `GuestSessions`). Only the hash is stored.
- Known gap: a malformed bearer token causes `Convert.FromBase64String` to throw → 500 instead of 401 (see `.claude/notes/homework.md`).

## Reports flow (async)

1. `GET /reports/generate` (API handler) creates a report request and puts a message on the SQS queue.
2. `reports-handler` consumes the message and computes the data.
3. `report-sender` consumes the result, creates the file (`ReportFileService`, QuestPDF/NPOI), stores it in S3, and sends an email (`EmailService`, SES).
4. `GET /reports` and `GET /reports/download` read history from the `ReportsHistory` table and S3.

## Known things to keep in mind

- Lambda memory is 128 MB and timeout 100 s for api-handler — keep heavy work out of it.
- Services are singletons inside a warm Lambda container, so do not store per-request state in service fields.
- `ResourceNames` throws when an env var is missing; a new env var must be added to `lambda_config.json` **and** to `ResourceNames`.
- Two docs have placeholders/TODOs: `CHANGELOG.md` is still the template, and `local-setup-guide.md` mentions `syndicate_aliases.yml` which is not in the repo.
