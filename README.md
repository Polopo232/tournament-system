# Tournament System

A system for organizing esports, chess and table tennis tournaments.
Organizers create tournaments, players register with a nickname, brackets and pairings are generated automatically,
and every player has a separate Elo rating for each game that changes after each match.

## Project structure

```text
TournamentSystem.sln
src/
  TournamentSystem.Domain/          Entities and enums
  TournamentSystem.Application/     Business logic (services, DTOs, interfaces, exceptions, security rules)
  TournamentSystem.Infrastructure/  EF Core DbContext, migrations, repositories, seed data
  TournamentSystem.Api/             ASP.NET Core Web API (controllers, JWT, Swagger, SignalR, ProblemDetails)
  TournamentSystem.Worker/          Worker Service (recalculates Elo and creates next rounds)
tests/
  TournamentSystem.UnitTests/       xUnit + Moq unit tests
tournament-web/                     React web client (Vite)
```

## Features

- **Tournament CRUD and querying:** search, filters (`status`, `discipline`), sorting (`sortBy`), pagination (`page`, `pageSize`).
- **Player nicknames:** every player has a login (`Username`) and a public nickname (`DisplayName`, unique, 3–20 characters). Nicknames are shown in brackets, matches and registrations.
- **Two tournament formats:**
  - `SingleElimination`: no draws, byes for odd player counts, winner is the last remaining player.
  - `RoundRobin`: every player meets every other player once. Pairings do not repeat between rounds.
- **Seeding by Elo:** players are seeded by their rating in the tournament's game. The seed order is stored (`SeedNumber`) and used for all later rounds.
- **Separate Elo per game:** a player has one rating for each game (for example `Chess`, `CS2`, `Dota2`, `TableTennis`), starting at 1200. Rating history is stored with the game name.
- **Winner:** the winner is saved when the tournament finishes and returned in the tournament details.
- **Ownership rules:** an Organizer can change only their own tournaments, their brackets and their match results. Admin and Service can act on any tournament.
- **Background Worker:** every 5 minutes it recalculates Elo for finished rounds and creates the next round.
- **Real-time updates:** match results are pushed to clients over WebSocket (SignalR).
- **Authentication and roles:** JWT Bearer tokens with `Admin`, `Organizer`, `Player` and `Service` roles.
- **Errors:** centralized `IExceptionHandler` returns RFC 7807 ProblemDetails for `400`, `401`, `403`, `404` and `500`.
- **Documentation:** Swagger UI with XML comments and request/response examples.

## Requirements

- .NET 10 SDK
- Node.js 20+
- SQL Server LocalDB (installed with Visual Studio)

## Ports

| Component | Address | Where it is set |
|---|---|---|
| API | `http://localhost:5000` | `--urls` option when starting the API (see below) |
| Frontend | `http://localhost:5173` | Vite default, allowed in CORS |
| Worker API address | `http://localhost:5000` | `ApiSettings:BaseUrl` in `src/TournamentSystem.Worker/appsettings.json` |

The API is started on port 5000 because the frontend (`tournament-web/src/api/client.js`) and the Worker expect it there.
The default port from `launchSettings.json` is 5195, so always pass `--urls` explicitly.

## Secrets

Secrets are not stored in the repository. They are kept with **user secrets** on the developer's machine.

```cmd
dotnet user-secrets init --project src/TournamentSystem.Api
dotnet user-secrets set "Jwt:Key" "<any string of at least 32 characters>" --project src/TournamentSystem.Api

dotnet user-secrets init --project src/TournamentSystem.Worker
dotnet user-secrets set "ApiSettings:Password" "Service123!" --project src/TournamentSystem.Worker
```

The API refuses to start if `Jwt:Key` is not set. The Worker refuses to start if `ApiSettings:Password` is not set.
Check the stored keys with `dotnet user-secrets list --project <project>`. Values are shown in the output, so do not paste them into public places.

## Database

SQL Server LocalDB is used by default. The connection string is in `src/TournamentSystem.Api/appsettings.json`.

```cmd
dotnet tool restore
dotnet build
dotnet ef database update --project src/TournamentSystem.Infrastructure --startup-project src/TournamentSystem.Api
```

On start, the API applies migrations. In the Development environment it also fills an empty database with test data.
Outside Development, only migrations are applied and no test accounts are created.

If the model has changed and the database no longer matches the migrations, drop the database and start the API again.
Dropping deletes all data.

```cmd
dotnet ef database drop --force --project src/TournamentSystem.Infrastructure --startup-project src/TournamentSystem.Api
```

### Test accounts (Development only)

| Username | Password | Role | Nickname |
|---|---|---|---|
| admin | Admin123! | Admin | Admin |
| organizer | Organizer123! | Organizer | Korraldaja |
| service | Service123! | Service | Teenus |
| player1 | Player123! | Player | Mari |
| player2 | Player123! | Player | Jaan |
| player3 | Player123! | Player | Kadri |
| player4 | Player123! | Player | Rasmus |
| player5 | Player123! | Player | Liis |
| player6 | Player123! | Player | Oskar |
| player7 | Player123! | Player | Anna |
| player8 | Player123! | Player | Tiit |

Usernames are used to log in. Nicknames are shown to other players.

## Build and run

```cmd
dotnet build
dotnet run --project src/TournamentSystem.Api --urls="http://localhost:5000"
dotnet run --project src/TournamentSystem.Worker
dotnet test
```

```cmd
cd tournament-web
npm install
npm run dev
```

Then open `http://localhost:5173`.

## Swagger and API usage

With the API running in Development mode, open `http://localhost:5000/swagger`.

### Authenticate in Swagger

1. Call `POST /api/auth/login` with a test account, for example `organizer` / `Organizer123!`.
2. Copy the `token` value from the response.
3. Click **Authorize** at the top right, paste the token and confirm.

### Rating and tournament rules

- A new player starts at 1200 in every game. Elo is calculated with K = 32. A draw counts as 0.5.
- The rating used for seeding and for updates is the rating in the tournament's game.
- In a single elimination tournament a draw is not allowed when a result is submitted.
- In a round robin tournament, a win gives 1 point and a draw gives 0.5 points. Byes give no points. The winner is the player with the most points. On equal points, the better seed wins.

### Main endpoints

| Method | Endpoint | Description | Access |
|---|---|---|---|
| `POST` | `/api/auth/register` | Register a player. Body: `username`, `displayName`, `email`, `password` | Public |
| `POST` | `/api/auth/login` | Log in and receive a JWT | Public |
| `GET` | `/api/auth/me` | Current user | Authenticated |
| `GET` | `/api/users` | All users | Admin |
| `PUT` | `/api/users/{id}/role` | Change a user's role | Admin |
| `GET` | `/api/tournaments` | List tournaments (`search`, `status`, `discipline`, `sortBy`, `page`, `pageSize`) | Public |
| `GET` | `/api/tournaments/{id}` | Tournament details with registrations, matches and winner | Public |
| `POST` | `/api/tournaments` | Create a tournament | Organizer, Admin |
| `PUT` | `/api/tournaments/{id}` | Update a tournament (own tournaments only, Organizer) | Organizer, Admin |
| `DELETE` | `/api/tournaments/{id}` | Delete a tournament (own tournaments only, Organizer) | Organizer, Admin |
| `POST` | `/api/tournaments/{id}/registrations` | Register the current player | Player, Organizer, Admin |
| `POST` | `/api/tournaments/{id}/bracket` | Generate the bracket and round 1, seeded by Elo | Organizer, Admin |
| `POST` | `/api/tournaments/{id}/rounds/next` | Create the next round or finish the tournament | Service, Organizer, Admin |
| `GET` | `/api/tournaments/rounds-completed` | Tournaments with completed rounds waiting for Elo recalculation | Service, Admin |
| `POST` | `/api/tournaments/{id}/ratings/recalculate` | Recalculate Elo for completed matches and save history | Service, Organizer, Admin |
| `PUT` | `/api/matches/{id}/result` | Submit a match result (own tournaments only, Organizer) | Organizer, Admin |
| `GET` | `/api/players/{id}/ratings` | Current rating in each game | Public |
| `GET` | `/api/players/{id}/rating-history` | Rating history in chronological order | Public |

## Worker

The Worker runs every 5 minutes (`ApiSettings:IntervalMinutes`). On each run it:

1. Logs in with the `service` account.
2. Gets the tournaments waiting for processing from `GET /api/tournaments/rounds-completed`.
3. Recalculates Elo with `POST /api/tournaments/{id}/ratings/recalculate`.
4. Creates the next round with `POST /api/tournaments/{id}/rounds/next`.

A tournament stays in the list until the next round exists. If a run fails, the tournament is processed again on the next run.
An error in one tournament does not stop the others.

## Real-time updates

The API provides a SignalR endpoint at `ws://localhost:5000/ws/tournament`.
The JWT is passed in the `access_token` query parameter. After a match result is saved, clients receive the `MatchResultSaved` event.

## Tests

```cmd
dotnet test
```

The unit tests cover:

- Elo calculation
- Bracket generation and seeding
- Round robin pairings (every pair meets exactly once)
- Detection of completed rounds for the Worker, including a failed next-round run
- Winner selection in both formats
- Match result handling
- Ownership rules for tournament management

There are no integration tests with a real database yet.

## Roadmap

- Show the tournament winner on the tournament page in the frontend
- Send the `RoundCreated` WebSocket event after a new round is created
- Import completed tournaments from an external service (Lichess)
- New frontend design with game tabs, filters in the URL and player profiles
- Integration tests with a real database