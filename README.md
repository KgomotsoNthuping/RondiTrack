# RondiTrack

**A full-stack application for managing stokvels, member contributions, savings cycles and payouts.**

RondiTrack provides a central place for stokvel members and administrators to manage their savings-group activity. It combines an ASP.NET Core Web API with a React frontend and a PostgreSQL database, with an emphasis on clearly defined business rules, authenticated access, accurate financial values and reliable data retrieval.

The application supports member and stokvel management, contribution tracking, cycle administration, payout rotation, arrears reporting and a personalised dashboard. Its API is also designed for consumers beyond the web application through versioned endpoints and an OpenAPI contract.

## Technology stack

| Area | Technologies |
| --- | --- |
| Backend | C# · .NET 10 · ASP.NET Core Web API |
| Data access | Entity Framework Core 10 · Npgsql · PostgreSQL |
| Authentication and security | JWT bearer authentication · Rotating refresh tokens · ASP.NET Core Identity password hashing · Policy-based authorization |
| API validation and documentation | FluentValidation · RFC 9457 Problem Details · OpenAPI · Scalar |
| Frontend | React 19 · TypeScript · Vite · Tailwind CSS |
| Frontend data and forms | TanStack Query · `openapi-fetch` · React Hook Form · Zod |
| Testing | .NET tests · PostgreSQL Testcontainers · Vitest · Mock Service Worker (MSW) |
| Operations | Docker Compose · GitHub Actions · OpenTelemetry · ASP.NET Core background services |

## Core functionality

### Stokvel and membership management

- Create, view and manage stokvels and their contribution settings.
- Maintain user accounts and stokvel membership relationships.
- Apply membership rules that prevent duplicate enrolment and protect users who still belong to a stokvel from inappropriate deletion.
- Use stokvel-specific roles: **Member**, **Treasurer** and **Admin**.
- Authorize access against the user's membership in the requested stokvel rather than relying on a single global role.

### Contribution cycles and payments

- Maintain contribution cycles with their own identifiers, numbers, targets and lifecycle status.
- Record a member's contribution against a specific stokvel and cycle.
- Allow Members to record their own contributions and Treasurers to record contributions on behalf of members of their stokvel.
- Restrict new contributions to open cycles and enforce payment and membership rules on the server.
- Prevent duplicate contributions for the same member and cycle through application checks and a PostgreSQL unique index.
- Use an `Idempotency-Key` to associate retries with the same intended contribution.

### Payouts and arrears

- Select payout recipients using the stokvel's membership rotation.
- Prevent the same cycle from being paid out more than once.
- Process payout-related changes through a database transaction.
- Close overdue cycles through a scheduled background service, calculate member shortfalls and create the next cycle.
- Track outstanding amounts and arrears in the member dashboard.

### Personalised web dashboard

- Sign in and view the stokvels associated with the current account.
- Switch between stokvels without mixing cached contribution histories.
- View cycle status, due dates, contribution history, payments and outstanding amounts.
- Browse paginated contributions and load additional history on demand.
- Record contributions through a validated form with amount prefilling and clear success or error feedback.
- Sign out while clearing the authenticated user's cached data.

## Architecture

RondiTrack separates HTTP handling, business decisions, domain models and data access.

```text
React / TypeScript frontend
          |
          | Versioned HTTP API
          v
ASP.NET Core controllers
          |
          +-- Request DTOs and FluentValidation
          +-- Authentication and stokvel authorization
          |
          v
Application services and domain rules
          |
          v
Repository interfaces
          |
          v
Entity Framework Core / Npgsql
          |
          v
PostgreSQL
```

The **Domain** layer contains entities such as `User`, `Stokvel`, `StokvelMember`, `ContributionCycle`, `Contribution` and `Payout`. Their constructors and methods enforce fundamental domain invariants.

The **API** layer exposes resource-oriented endpoints, maps request and response DTOs, applies validation and access control, and coordinates business services. Mapping is explicit so the public contract remains separate from the persistence model.

The **Infrastructure** layer implements repository interfaces using EF Core. An in-memory implementation also exists for isolated development and testing scenarios, while PostgreSQL is the primary persistent store.

The **web** application consumes generated OpenAPI types through a shared authenticated client. TanStack Query manages retrieval, caching and mutations; UI components focus on displaying data and collecting input.

### Repository layout

```text
RondiTrack/
├── Api/                  ASP.NET Core API, controllers, DTOs and services
│   ├── Auth/             Authentication and authorization
│   ├── Background/       Cycle closing and housekeeping jobs
│   ├── Controllers/      Versioned HTTP endpoints
│   ├── DTO/              Request and response contracts
│   ├── ErrorHandling/    Centralised API error handling
│   ├── Mappings/         Domain and DTO conversions
│   ├── Observability/    Tracing, metrics and correlation
│   └── Validation/       Request validators
├── Api.Tests/            Backend and integration tests
├── Domain/               Entities, business rules and repository contracts
├── Infrastructure/       EF Core persistence and repository implementations
├── web/                  React frontend, API hooks, forms and frontend tests
├── compose.yaml          Local container orchestration
└── .github/workflows/     Continuous integration configuration
```

## Backend API

API endpoints use the `/api/v1` prefix. Representative routes include:

| Method | Endpoint | Purpose |
| --- | --- | --- |
| `POST` | `/api/v1/auth/register` | Register an account |
| `POST` | `/api/v1/auth/login` | Sign in and obtain an access token |
| `POST` | `/api/v1/auth/refresh` | Rotate the refresh token and issue a new access token |
| `POST` | `/api/v1/auth/logout` | End the current session |
| `GET` | `/api/v1/me` | Retrieve the authenticated member's profile |
| `GET` | `/api/v1/me/home` | Retrieve a personalised stokvel summary |
| `GET` | `/api/v1/stokvels/{stokvelId}/cycles` | List a stokvel's contribution cycles |
| `GET` | `/api/v1/stokvels/{stokvelId}/contributions` | Browse contributions with pagination |
| `GET` | `/api/v1/stokvels/{stokvelId}/cycles/{cycleId}/contributions` | Browse contributions for one cycle |
| `POST` | `/api/v1/stokvels/{stokvelId}/cycles/{cycleId}/contributions` | Record a cycle contribution |

Additional controllers handle users, stokvel records, membership operations, cycle management and payouts. See the OpenAPI document for the complete route and request/response definitions.

### Validation and error responses

FluentValidation handles request structure and field-level constraints. The application service and domain layers enforce rules requiring existing state, such as membership eligibility, duplicate payments and payout eligibility.

Expected API failures use standard HTTP statuses and Problem Details responses:

| Status | Meaning |
| --- | --- |
| `400` | Invalid request or missing required input |
| `401` | Authentication required or session no longer valid |
| `403` | Authenticated user lacks the required permission |
| `404` | Resource unavailable within the requested access scope |
| `409` | Conflicting state, duplicate record or stale update |
| `422` | Request violates a business rule |
| `429` | Request rate limit reached; check `Retry-After` |
| `500` | Unexpected server error |

Problem responses include a correlation identifier so support and development logs can be matched to an unsuccessful request.

### Authentication and authorization

The API uses JWT bearer access tokens and rotating refresh tokens. Passwords are processed with ASP.NET Core's password hasher. Refresh tokens are stored as hashes on the server and sent to the browser through an HttpOnly cookie rather than returned as ordinary application data.

The frontend keeps the access token in memory. Its authenticated fetch implementation refreshes the session when needed and replays an interrupted request once using the renewed token. Concurrent refresh requests share the same refresh operation.

Stokvel authorization is resolved against membership stored in PostgreSQL. This means the same account may have different roles in different stokvels, and access decisions use the currently requested stokvel.

The API also applies configurable rate limits to login, contribution and payout operations. Browser access is controlled through a configured CORS policy that supports credentials and the required request headers.

## Data persistence and consistency

RondiTrack uses EF Core migrations to manage its PostgreSQL schema. Primary entities are connected through explicit relationships and foreign keys.

Important persistence decisions include:

- **Relational membership:** `StokvelMember` represents the relationship between a user and a stokvel and stores the member's role.
- **Unique records:** composite indexes protect rules such as one contribution per member and cycle and one payout per cycle.
- **Financial precision:** persistent amounts use PostgreSQL decimal-compatible values; the web-facing contribution endpoint accepts integer cents.
- **Pagination:** database-side filtering, sorting and token-based pagination keep larger lists manageable.
- **Concurrent updates:** version values backed by PostgreSQL's `xmin` concurrency mechanism detect stale writes.
- **Payout transactions:** related payout and cycle-status changes are committed as one database operation.

Database queries can be inspected through EF Core logs, SQL scripts and PostgreSQL `EXPLAIN ANALYZE` to evaluate query plans and indexing decisions.

## Background processing and observability

The application includes background services for stokvel lifecycle administration and housekeeping.

**Cycle closing** uses the South African calendar (`Africa/Johannesburg`) to identify completed contribution periods. It closes eligible cycles, records arrears and opens subsequent cycles. Database-level protections support repeatable processing without duplicate cycle and arrears records.

**Housekeeping** removes eligible expired refresh-token and idempotency records according to configured retention periods, using database-side deletion operations.

**Observability** is provided through OpenTelemetry traces, metrics and structured logs. Custom measurements cover contribution recording, payout activity, access refusals and cycle processing. Health endpoints support process and database readiness checks:

- `GET /health/live`
- `GET /health/ready`

Telemetry and application logs are designed to avoid exposing unnecessary member or financial information.

## Frontend

The React application uses TypeScript and a shared `openapi-fetch` client, with its type definitions generated from the backend's OpenAPI specification.

### Data retrieval and caching

TanStack Query handles the authenticated member profile, home summary, cycles and contribution history. Query keys include the stokvel identifier for stokvel-specific information. Switching stokvels therefore retrieves data for the selected group without displaying another group's cached values.

Successful contribution recording invalidates the relevant home, cycle and contribution queries so displayed figures are refreshed from the API rather than guessed locally.

### Contribution form and financial values

React Hook Form and Zod validate member selection and the entered amount. The form is available for open contribution cycles; Members can record for themselves, while Treasurers can select another stokvel member.

The frontend converts editable rand amounts to integer cents without floating-point money multiplication or implicit rounding. For example, `150.50` becomes `15050` cents, while values requiring rounding are rejected. Currency display uses South African rand formatting.

The form creates an idempotency key for a payment intent and retains it after network uncertainty or applicable retry responses. The key is replaced when the payment details change or a successful contribution completes. Server errors are displayed with field-level messages, readable explanations and available correlation references.

## Getting started

### Prerequisites

For local development:

- .NET 10 SDK
- Node.js 24 and npm
- PostgreSQL, with a local database and application user
- Git

Docker Desktop can also be used with the repository's Compose configuration.

### Backend setup (Windows PowerShell)

1. Clone the repository and restore the API dependencies:

```powershell
git clone <repository-url>
cd RondiTrack
dotnet restore .\Api\Api.csproj
```

2. Create the local PostgreSQL database and a dedicated user. A development example is:

```sql
CREATE ROLE ronditrack WITH LOGIN PASSWORD 'REPLACE_WITH_LOCAL_PASSWORD';
CREATE DATABASE ronditrack_dev OWNER ronditrack;
```

3. Configure secrets for the API project:

```powershell
dotnet user-secrets init --project .\Api\Api.csproj
dotnet user-secrets set "ConnectionStrings:RondiTrack" "Host=127.0.0.1;Port=5432;Database=ronditrack_dev;Username=ronditrack;Password=REPLACE_WITH_LOCAL_PASSWORD" --project .\Api\Api.csproj
dotnet user-secrets set "Jwt:SigningKey" "REPLACE_WITH_A_RANDOM_KEY_OF_AT_LEAST_32_BYTES" --project .\Api\Api.csproj
```

Provide any additional configured JWT issuer, audience and development seed settings through the appropriate environment configuration. Do not commit passwords, tokens or signing keys.

4. Apply migrations and run the API:

```powershell
dotnet ef database update --project .\Api\Api.csproj
dotnet run --project .\Api\Api.csproj
```

The default local HTTP address is **http://localhost:5227**. With the API running in Development, documentation is available at:

- OpenAPI: `http://localhost:5227/openapi/v1.json`
- Scalar: `http://localhost:5227/scalar/v1`

### Frontend setup

In a second terminal:

```powershell
cd web
npm.cmd install
```

Create `web/.env.local` with the following value:

```dotenv
VITE_API_BASE_URL=http://localhost:5227
```

Generate TypeScript definitions from the running API when the contract changes:

```powershell
npx.cmd openapi-typescript http://localhost:5227/openapi/v1.json -o src/api/schema.d.ts
```

Start the frontend:

```powershell
npm.cmd run dev
```

Open **http://localhost:3000** and sign in with an account configured in the development environment.

### Docker Compose

The repository includes a containerised API and PostgreSQL setup. Copy the supplied environment template, set the required values locally and start the services:

```powershell
Copy-Item .env.example .env
docker compose up --build -d
```

To inspect services or stop the stack:

```powershell
docker compose ps
docker compose logs -f
docker compose down
```

Keep `.env` and any other secret-containing configuration out of version control.

## Testing and quality checks

### Backend

The .NET test project covers API behaviour and database integration, including business rules, validation, authorization, data access, concurrency and background processing. PostgreSQL-backed integration tests use Testcontainers, so Docker must be available for those tests.

From the repository root:

```powershell
dotnet build .\Api\Api.csproj
dotnet test .\Api.Tests\Api.Tests.csproj
```

### Frontend

Vitest tests cover presentation utilities, currency parsing and round trips, validation schemas, session refresh and contribution-retry behaviour. MSW simulates HTTP responses while exercising the application's real request code.

From `web`:

```powershell
npx.cmd vitest run
npm.cmd run build
npm.cmd run lint
```

The project also includes a GitHub Actions workflow for automated checks on repository changes.

## Engineering principles

RondiTrack follows a few consistent implementation principles:

- **Business rules belong in domain objects and services**, not duplicated across controllers or UI components.
- **API contracts are explicit**, with dedicated DTOs, validation and generated frontend types.
- **Authorization depends on stokvel context**, not solely on an account-wide role.
- **Financial values must be exact**, using decimal persistence and integer cents at the frontend boundary.
- **Database constraints complement application checks**, particularly when requests overlap.
- **Client state should reflect server-confirmed data**, especially when recording money.
- **Automated tests cover both unit behaviour and HTTP/database integration**, including failure and retry scenarios.

---

**RondiTrack** brings together a structured .NET backend and a responsive React interface to support accountable stokvel administration and clear visibility of member contributions.
