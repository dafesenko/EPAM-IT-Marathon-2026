# Restaurant Backend App — Project Context

This file is the entry point for working in the backend. Detailed notes live in `.claude/notes/`.

## What this is

Serverless REST API for a restaurant booking and ordering platform (IT Marathon 2026 project).
Customers reserve tables, pre-order dishes, and organize group reservations with guest sessions.
Waiters manage tables and orders for their location. Managers/admins manage locations, dishes, tables, and reports.

Stack:
- **.NET 10** (C#, `Nullable` enabled), AWS Lambda, API Gateway (proxy integration)
- **DynamoDB** (via `Amazon.DynamoDBv2.DataModel`), **Cognito** (auth, groups = roles), **S3** (images, profile pictures, report files), **SQS** (report generation queue), **SES** (report emails)
- **AWS Syndicate** (Python tool) for packaging and deployment — config in `.syndicate-config-restaurant-backend-app/syndicate.yml`
- PDF/Excel generation: QuestPDF, NPOI. Images: ImageSharp.

## Folder map

```
restaurant-backend-app/
├── dnapp/lambdas/
│   ├── api-handler/        # Main API Lambda (≈245 source files). Most work happens here.
│   ├── report-sender/      # SQS-triggered Lambda: builds report files and emails them (SES)
│   └── reports-handler/    # SQS-triggered Lambda: report generation worker
├── Tests/                  # xUnit + Moq + AutoFixture tests for api-handler services
├── docs/                   # ApiRoutes.md (API reference), Diagrams.md, GenerateSwagger.md, drawio/svg diagrams
├── swagger_src/            # Swagger UI static files
├── local-setup-guide.md    # AWS + Syndicate setup steps
├── deployment_resources.json  # Syndicate resource definitions (API Gateway routes, IAM policy, tables, etc.)
├── CHANGELOG.md            # Keep a Changelog template (not filled in yet)
└── syndicate_venv/         # Python venv for Syndicate — ignore, never edit
```

Build output (`bin/`, `obj/`) and `.syndicate-config-*/bundles` are generated — do not edit.

## api-handler layering

Request flow:
`Function.cs` (entry, `FunctionHandler`) → `Routing/RouteRegistry.cs` (route table, DI wiring) → `Handlers/**` (one class per endpoint, implements `IApiRequestHandler`) → `Services/**` (business rules, interface + impl) → `Repositories/**` (DynamoDB access, interface + impl) → DynamoDB / S3 / Cognito.

Other folders:
- `Models/` — request/response DTOs (JSON shape of the API)
- `Entities/` — DynamoDB entities (`[DynamoDBTable]`, `[DynamoDBHashKey]`, GSI attributes)
- `Mappers/` — entity ↔ response mapping
- `Constants/` — enums (with `*Labels` helpers), table/index names, `ResourceNames` (reads env vars)
- `Exceptions/` — domain exceptions, translated to HTTP responses by `Services/ExceptionHandler.cs`
- `Helpers/` — Cognito ID token validation, request body parsing, role checks (`RoleHelper`), user context, validation, occupancy/equality helpers, `ResponseBuilder`
- `Converters/` — `EnumAsIntConverter<T>` for storing enums as ints in DynamoDB
- `Routing/Route.cs`, `Routing/RouteHandler.cs` — route matching with `{param}` placeholders; `Records/` holds small records used by routing and reservation context

Dependencies are registered as singletons in `RouteRegistry.BuildRoutes()`. A new repository or service must be registered there, and a new handler must be added to the matching `Get*Routes()` list.

## Conventions observed in the code

- Handlers are thin: parse request, call a service, build the response.
- Services hold business rules and throw typed exceptions; never return HTTP codes from services.
- Repositories own all DynamoDB calls; use `DynamoConfig.*Config(tableName)` helpers for config. Reads use `ConsistentRead = true`.
- Table and index names come from environment variables set in `lambda_config.json`, read through `ResourceNames`. Do not hard-code table names in code (except `GroupReservations` / `GuestSessions`, which are set as literals in `lambda_config.json`).
- Enums are stored as ints in DynamoDB (`EnumAsIntConverter`), but JSON uses the names/labels — check the Labels classes before changing values.
- Auth: Cognito ID token in the `Authorization` header; role checks via `RoleHelper`; user identity via `UserContextHelper`. Guest (non-logged-in) access uses session tokens (`GuestAuthorizationService`).
- Feature areas (Services/Repositories/Handlers each have a folder): Auth/Users, Locations, Tables, Waiters, Dishes, Reservations, Orders, GroupReservations, GuestSessions, Feedbacks, Notifications, Reports, TimeSlots.

## Build, test, deploy

```powershell
# from restaurant-backend-app/
dotnet build .\restaurant-backend-app.sln
dotnet test .\Tests\APIHandler.Tests.csproj

syndicate build
syndicate deploy
```

Deployment requires AWS credentials (`aws configure`) and a `SDCT_CONF` env var pointing to `.syndicate-config-restaurant-backend-app` — see `local-setup-guide.md`.

Swagger: see `docs/GenerateSwagger.md` (export from API Gateway → `specs.json` → `syndicate generate swagger-ui`).

## Where to look first

- Endpoint list and request/response examples: `docs/ApiRoutes.md`
- Route → handler mapping: `dnapp/lambdas/api-handler/Routing/RouteRegistry.cs`
- Data model and state machines: `.claude/notes/data-model.md` (and `docs/Diagrams.md`)
- Architecture summary: `.claude/notes/architecture.md`
- Homework context (to be filled when the homework folder arrives): `.claude/notes/homework.md`

## Ground rules for work in this repo

- Do not edit generated output (`bin/`, `obj/`, `.syndicate-config-*/bundles`, `syndicate_venv/`).
- Do not commit or change AWS settings; `syndicate.yml` and `deployment_resources.json` contain account-specific values.
- Keep new code in the same style as neighbouring files (naming, folder placement, one handler/service/repository per feature).
- Add or update tests in `Tests/Services/` when changing service logic.
- Update `docs/ApiRoutes.md` when a route or its JSON shape changes.
