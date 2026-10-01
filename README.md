# RondiTrack

RondiTrack is a Web API that manages stokvels and their members.

## API Structure Choice

Controllers were selected because they provide clear separation between HTTP routing and domain logic. It also makes it easy to show attribute routing.

## Domain Rules

### User

A User must:

- Have a non-empty full name.
- Have an email address.
- Have a phone number.

### Stokvel

A Stokvel must:

- Have a name.
- Have a monthly contribution greater than zero.
- Have at least one contribution period.
- Have a current period within the total number of periods.

### Membership

- A User cannot be added to the same Stokvel more than once.
- A User cannot be deleted while they are still a member of a stokvel.

### Contribution

- A contribution must belong to an existing stokvel, user and contribution cycle.
- The user must be a member of the stokvel.
- Duplicate contributions for the same member and cycle are not allowed.

### Payout

- A payout belongs to a stokvel and contribution cycle.
- A contribution cycle can only be paid out once.
- The next recipient is selected from the stokvel member rotation.
- The payout and cycle status update must succeed or fail together.

## Data Storage

The application originally used an in-memory store.

For Assignment 5.1, RondiTrack now uses PostgreSQL with Entity Framework Core.

The database contains:

- Users
- Stokvels
- StokvelMembers
- ContributionCycles
- Contributions
- Payouts

`ITrackStore` was kept as the storage abstraction, while `EfTrackStore` now uses `RondiTrackDbContext` to read and write data.

Data now remains available after the API is restarted.

## PostgreSQL Setup

PostgreSQL was installed locally.

The development database is:

- Database: `ronditrack_dev`
- User: `ronditrack`
- Port: `5432`

Connectivity was tested independently before EF Core was added:

```bash
psql -h localhost -p 5432 -U ronditrack -d ronditrack_dev
```

```sql
SELECT current_database(), current_user;
```

## How-to-use

1. Clone the repository.

2. Navigate to the project:

```bash
cd RondiTrack
```

3. Restore dependencies:

```bash
dotnet restore
```

4. Configure the PostgreSQL connection using User Secrets:

```bash
dotnet user-secrets init --project .\Api\Api.csproj
```

```bash
dotnet user-secrets set "ConnectionStrings:RondiTrack" "Host=127.0.0.1;Port=5432;Database=ronditrack_dev;Username=ronditrack;Password=YOUR_PASSWORD;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=50" --project .\Api\Api.csproj
```

5. Apply the migration:

```bash
dotnet ef database update --project .\Api\Api.csproj
```

6. Run:

```bash
dotnet run --project .\Api\Api.csproj
```

## DTO

The application does not expose domain entities directly through the HTTP API.

Create and update operations use request DTOs, while responses use separate response DTOs. This prevents clients from setting internal values such as IDs and allows the API contract to change independently from the internal domain model.

Mapping is performed manually through dedicated mapping classes.

## Service Layer

The service layer is used for operations that contain meaningful business decisions.

These currently include:

- Adding and removing stokvel members.
- Preventing deletion of users who still belong to a stokvel.
- Recording contributions.
- Preventing duplicate contributions.
- Handling contribution idempotency.
- Processing payouts.
- Updating a contribution cycle during payout processing.

## Contribution Idempotency

Recording a contribution requires an `Idempotency-Key` request header.

If the same key is sent again with the same request, the original contribution is returned.

If the same key is reused with different request data, the API returns `409 Conflict`.

A separate duplicate-contribution check prevents the same member's contribution for the same stokvel cycle from being recorded twice.

## 400, 409 and 422

`400 Bad Request` is used when the request does not satisfy the API contract.

`409 Conflict` is used when the request conflicts with existing system state, such as a duplicate contribution or an already-paid contribution cycle.

`422 Unprocessable Entity` is used when the request is valid but breaks a business rule.

## EF Core and Migration

RondiTrack uses one `RondiTrackDbContext` for:

- User
- Stokvel
- StokvelMember
- ContributionCycle
- Contribution
- Payout

The initial migration was created with:

```bash
dotnet ef migrations add InitialPostgreSql --project .\Api\Api.csproj
```

The migration was reviewed before it was applied. The review checked the six expected tables, keys, decimal precision, indexes and that there were no unexpected destructive operations.

`Stokvel.Members` remains a read-only domain collection and is not mapped directly by EF Core. Membership is stored through `StokvelMember`.

## Secret Management

The PostgreSQL password is not stored in Git-tracked configuration files.

The connection string is stored using .NET User Secrets.

## Npgsql Pooling and Retry

Connection pooling is enabled.

The API uses:

```csharp
EnableRetryOnFailure(
    maxRetryCount: 3,
    maxRetryDelay: TimeSpan.FromSeconds(5),
    errorCodesToAdd: null);
```

Three retries with a maximum delay of five seconds allow short-lived database or network failures to recover without delaying requests for too long.

Temporary connectivity failures may be retried. Invalid credentials, validation failures and database constraint violations should not be treated as transient failures.

## Dependency Injection Lifetime

The in-memory repository previously used a Singleton lifetime.

The EF Core repository and service now use Scoped lifetimes because `RondiTrackDbContext` is scoped to a request.

```csharp
builder.Services.AddScoped<ITrackStore, EfTrackStore>();
builder.Services.AddScoped<ITrackService, TrackService>();
```

## Payout Transaction

Payout processing uses an explicit database transaction.

The operation:

1. Finds the next eligible member.
2. Creates the payout.
3. Saves the payout.
4. Marks the contribution cycle as paid out.
5. Saves the cycle update.
6. Commits the transaction.

If part of the process fails, the transaction is rolled back.

The rollback test deliberately fails after the payout insert and then uses a new DbContext to confirm that:

- No partial payout remains.
- The contribution cycle is still open.

## Test Run Output

Before the PostgreSQL change:

```text
Test summary: total: 12, failed: 0, succeeded: 12, skipped: 0
```

After the PostgreSQL change:

```text
Test summary: total: 13, failed: 2, succeeded: 11, skipped: 0
```

The two current failures are:

- `RecordContribution_WhenSameRequestIsRetried_ReturnsIdenticalResponse`
- `RecordContribution_WhenIdempotencyKeyIsReusedWithDifferentPayload_Returns409ProblemJson`

Both currently return `409 Conflict` where the first request expects `201 Created`.

This has been recorded as a PostgreSQL test-isolation issue and will be corrected separately.

## Definition of Done

### Users

#### GET /api/users

- Documentation: Complete
- Validation: Not required because there is no request body
- Integration testing: Covered
- Status codes reviewed: Yes
- Expected response: 200 OK

#### POST /api/users

- Documentation: Complete
- Validation: Covered by FluentValidation
- Integration testing: Covered
- Status codes reviewed: Yes
- Expected responses:
  - 201 Created
  - 400 Bad Request

#### PUT /api/users/{id}

- Documentation: Complete
- Validation: Covered by FluentValidation
- Integration testing: Covered
- Status codes reviewed: Yes
- Expected responses:
  - 200 OK
  - 400 Bad Request
  - 404 Not Found

#### DELETE /api/users/{id}

- Documentation: Complete
- Integration testing: Covered
- Status codes reviewed: Yes
- Expected responses:
  - 204 No Content
  - 404 Not Found
  - 409 Conflict

### Stokvels

#### GET /api/stokvels

- Documentation: Complete
- Integration testing: Covered
- Status codes reviewed: Yes
- Expected response: 200 OK

#### POST /api/stokvels

- Documentation: Complete
- Validation: Covered by FluentValidation
- Integration testing: Covered
- Status codes reviewed: Yes
- Expected responses:
  - 201 Created
  - 400 Bad Request

#### PUT /api/stokvels/{id}

- Documentation: Complete
- Validation: Covered by FluentValidation
- Integration testing: Covered
- Status codes reviewed: Yes
- Expected responses:
  - 200 OK
  - 400 Bad Request
  - 404 Not Found

#### DELETE /api/stokvels/{id}

- Documentation: Complete
- Integration testing: Covered
- Status codes reviewed: Yes
- Expected responses:
  - 204 No Content
  - 404 Not Found

### Contributions

#### GET /api/stokvels/{stokvelId}/contributions/{contributionId}

- Documentation: Complete
- Integration testing: Covered
- Status codes reviewed: Yes
- Expected responses:
  - 200 OK
  - 404 Not Found

#### POST /api/stokvels/{stokvelId}/contributions

- Documentation: Complete
- Validation: Covered by FluentValidation and `Idempotency-Key`
- Integration testing: Covered
- Status codes reviewed: Yes
- Expected responses:
  - 201 Created
  - 400 Bad Request
  - 404 Not Found
  - 409 Conflict
  - 422 Unprocessable Entity

### Payouts

#### POST /api/stokvels/{stokvelId}/cycles/{cycleId}/payout

- Documentation: Complete
- Integration testing: Rollback behaviour covered
- Status codes reviewed: Yes
- Expected responses:
  - 201 Created
  - 404 Not Found
  - 409 Conflict
  - 422 Unprocessable Entity

### Assignment 5.1 DoD Extension

| Area | Persisted via EF Core | Explicit transaction tested |
|---|---|---|
| Users | Yes | N/A |
| Stokvels | Yes | N/A |
| StokvelMembers | Yes | N/A |
| ContributionCycles | Yes | N/A |
| Contributions | Yes | N/A |
| Payouts | Yes | Yes |

## Known Issues

- Two contribution integration tests currently fail because PostgreSQL test data is not yet fully isolated between runs.
- The test build reports an EF Core package-version warning that should be aligned during cleanup.