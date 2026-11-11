# Tournament System

A system for organizing esports, chess and table tennis tournaments.
Organizers create tournaments, players register, brackets and pairings are generated automatically,
and every player has an Elo rating that changes after each match.

## Project structure

```text
TournamentSystem.sln
src/
  TournamentSystem.Domain/          Entities and enums
  TournamentSystem.Application/     Business logic (services, DTOs, interfaces, exceptions)
  TournamentSystem.Infrastructure/  EF Core DbContext, migrations, repositories
  TournamentSystem.Api/             ASP.NET Core Web API (controllers, JWT, Swagger, ProblemDetails middleware)
  TournamentSystem.Worker/          Worker Service (recalculates Elo and creates next rounds)
tests/
  TournamentSystem.UnitTests/       xUnit unit tests
tournament-web/                     React web client (Vite)
```

## Key Features & API Capabilities

- **OpenAPI / Swagger UI:** Full XML documentation and request/response examples enabled.
- **JWT Authentication & Role Authorization:** Supports `Admin`, `Organizer`, `Player`, and `Service` roles via Bearer tokens in Swagger UI.
- **Global Error Handling:** Centralized `IExceptionHandler` returning standardized **RFC 7807 ProblemDetails** (`400`, `401`, `403`, `404`, `500`) and structured event logging (`ILogger`).
- **CORS Configured:** Pre-configured for the React frontend (`http://localhost:5173`).
- **Tournament CRUD & Querying:** Full support for search, filtering (`status`, `discipline`), sorting (`sortBy`), and pagination (`page`, `pageSize`).

## Requirements

- .NET 10 SDK
- Node.js 20+

## Database

SQL Server LocalDB is used by default (connection string in `src/TournamentSystem.Api/appsettings.json`).

```bash
dotnet tool restore
dotnet ef database update --project src/TournamentSystem.Infrastructure --startup-project src/TournamentSystem.Api
```

The database is migrated and filled with test data automatically when the API starts.

### Test accounts

| Username | Password | Role |
|---|---|---|
| admin | Admin123! | Admin |
| organizer | Organizer123! | Organizer |
| service | Service123! | Service |
| player1 ... player8 | Player123! | Player |

## Build and run

```bash
dotnet build
dotnet run --project src/TournamentSystem.Api
dotnet run --project src/TournamentSystem.Worker
dotnet test
```

```bash
cd tournament-web
npm install
npm run dev
```

## Swagger & API Usage

Once `TournamentSystem.Api` is running in Development mode, open:
- **Swagger UI:** `http://localhost:<port>/swagger`

### How to authenticate in Swagger:
1. Execute `POST /api/auth/login` with one of the test accounts (e.g., `organizer` / `Organizer123!`).
2. Copy the `token` string from the response.
3. Click the **Authorize** button at the top right of Swagger UI, paste the token, and click **Authorize**.

### Main Endpoints

| Method | Endpoint | Description | Access |
|---|---|---|---|
| `POST` | `/api/auth/register` | Register a new player account | Public |
| `POST` | `/api/auth/login` | Authenticate and receive JWT token | Public |
| `GET` | `/api/auth/me` | Get current authenticated user info | Authenticated |
| `GET` | `/api/users` | Get all users | Admin |
| `PUT` | `/api/users/{id}/role` | Change user role | Admin |
| `GET` | `/api/tournaments` | List tournaments (`search`, `status`, `discipline`, `sortBy`, `page`, `pageSize`) | Public |
| `GET` | `/api/tournaments/{id}` | Get tournament details by ID | Public |
| `POST` | `/api/tournaments` | Create a new tournament | Organizer, Admin |
| `PUT` | `/api/tournaments/{id}` | Update tournament details | Organizer, Admin |
| `DELETE` | `/api/tournaments/{id}` | Delete a tournament | Organizer, Admin |