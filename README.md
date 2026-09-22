# RondiTrack
RondiTrack is a Web API that manages stokvels and it's members.

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