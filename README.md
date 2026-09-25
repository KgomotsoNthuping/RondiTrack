# RondiTrack
RondiTrack is a Web API that manages stokvels and it's members.

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

## Data Storage
- The application currently uses an in-memory store.
- Seed data is a ubuntu savers stokvel with a R500 monthly contribution 

## How-to-use
1. Clone the repository.

2. Navigate to the project:
   cd RondiTrack.Api

3. Restore dependencies:
   dotnet restore

4. Run:
   dotnet run

## DTO
The application no longer exposes domain entities directly through the HTTP API.

Create and update operations use request DTOs, while responses use separate response DTOs. This prevents clients from attempting to set internal values such as IDs or creation timestamps and allows the API contract to evolve independently from the internal domain model.

Mapping is performed manually through dedicated mapping classes.

Manual mapping is appropriate for RondiTrack because the mappings are small and explicit. It also makes it easy to audit exactly which fields cross the HTTP boundary, which is particularly valuable for an application that manages financial information.

## Service Layer
The service layer is used only for operations that contain meaningful business decisions.

These currently include:

- Adding and removing stokvel members.
- Preventing deletion of users who still belong to a stokvel.
- Recording contributions.
- Preventing duplicate contributions.
- Enforcing contribution amounts and cycle membership.
- Handling idempotency for contribution recording.

Simple retrieval operations continue to access the in-memory store directly because they do not require business decisions.

## Contribution Idempotency
Recording a contribution requires an Idempotency-Key request header.

When a contribution is successfully recorded, the key, request details and original contribution are stored in memory.

If the same key is sent again with the same request, the original contribution is returned instead of recording another payment.

If the same key is reused with different request data, the API returns 409 Conflict.

A separate duplicate-contribution check also prevents a member's contribution for the same stokvel cycle from being recorded twice even when a different idempotency key is supplied.

## 400 and 422
RondiTrack uses 400 Bad Request when the request itself does not satisfy the API contract, such as a contribution request without an Idempotency-Key.

422 Unprocessable Entity is used when the request can be understood but cannot be processed because it violates a domain or business rule, such as submitting R100 when the stokvel requires a R500 contribution.

409 Conflict is used when the request conflicts with existing system state, such as attempting to record a contribution that has already been recorded.