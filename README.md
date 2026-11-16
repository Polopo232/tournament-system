# Tournament System

A web system for organizing esports, chess and table tennis tournaments. Organizers create tournaments, players register with a nickname, brackets and pairings are generated automatically, and every player has a separate Elo rating for each game.

## Features

- Tournament CRUD with search, filters (`status`, `discipline`), sorting (`sortBy`) and pagination
- Player nicknames (`DisplayName`) shown across the system
- Single elimination and round robin formats
- Seeding by Elo rating in the tournament's game
- Match results with automatic winner determination and byes for odd player counts
- Separate Elo rating per game (K = 32) with full rating history
- Background Worker that recalculates Elo and creates the next round
- Real-time updates over WebSocket (SignalR)
- JWT authentication with roles `Admin`, `Organizer`, `Player`, `Service`
- RFC 7807 ProblemDetails for all errors
- Swagger / OpenAPI documentation with XML comments

## Project structure

```text
TournamentSystem.sln
src/
  TournamentSystem.Domain/          Entities and enums
  TournamentSystem.Application/     Business logic (services, DTOs, interfaces, exceptions)
  TournamentSystem.Infrastructure/  EF Core DbContext, migrations, repositories
  TournamentSystem.Api/             ASP.NET Core Web API (controllers, JWT, Swagger, SignalR)
  TournamentSystem.Worker/          Worker Service (recalculates Elo and creates next rounds)
tests/
  TournamentSystem.UnitTests/       xUnit + Moq unit tests
tournament-web/                     React web client (Vite)
```

## Requirements

- .NET 10 SDK
- Node.js 20+
- SQL Server LocalDB (installed with Visual Studio)

## Ports

| Component | Address | Where it is set |
|---|---|---|
| API | `http://localhost:5195` by default | `src/TournamentSystem.Api/Properties/launchSettings.json` |
| API (used by the frontend) | `http://localhost:5000` | `--urls` option, see below |
| Frontend | `http://localhost:5173` | Vite default, allowed in CORS |
| Worker API address | `ApiSettings:BaseUrl` | `src/TournamentSystem.Worker/appsettings.json` |

The frontend (`tournament-web/src/api/client.js`) and the SignalR connection expect the API on `http://localhost:5000`. Start the API on that address:

```bash
dotnet run --project src/TournamentSystem.Api --urls="http://localhost:5000"
```

If you start the API on another port, change `ApiSettings:BaseUrl` in the Worker's `appsettings.json` to the same address, and update the frontend address as well.

## Setup

### 1. Restore tools and build

```bash
dotnet tool restore
dotnet build
```

### 2. Database

The database is created and migrated automatically when the API starts. Test data is inserted on the first start, when the `Users` table is empty.

To apply migrations manually:

```bash
dotnet ef database update --project src/TournamentSystem.Infrastructure --startup-project src/TournamentSystem.Api
```

If the model has changed and the database is out of date, recreate it:

```bash
dotnet ef database drop --force --project src/TournamentSystem.Infrastructure --startup-project src/TournamentSystem.Api
```

After dropping, starting the API recreates the database and fills it with test data. Do not drop the database if you need to keep your own data.

### 3. Run the API, Worker and frontend

```bash
dotnet run --project src/TournamentSystem.Api --urls="http://localhost:5000"
dotnet run --project src/TournamentSystem.Worker
```

```bash
cd tournament-web
npm install
npm run dev
```

Open `http://localhost:5173` in the browser.

## Test accounts

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

Usernames are used for login. Nicknames are shown to other users.

## Ratings (Elo)

- Each player has a separate rating for each game (for example `Chess`, `CS2`, `Dota2`, `TableTennis`). A new player starts at 1200 in every game.
- The formula is the standard Elo with K = 32. A draw counts as 0.5.
- The rating used for seeding and for updating is the one for the tournament's game.
- Every rating change is saved in the rating history with the game name.

## Tournament flow

1. An Organizer creates a tournament. Its status is `Registration`.
2. Players register for the tournament.
3. The Organizer generates the bracket with `POST /api/tournaments/{id}/bracket`. The status becomes `InProgress` and round 1 is created. Players are seeded by their rating in the tournament's game.
4. The Organizer submits match results with `PUT /api/matches/{id}/result`.
5. When all matches of a round are completed, the Worker recalculates Elo and creates the next round, or finishes the tournament.

Formats:
- **SingleElimination**: draws are not allowed. Players who win advance to the next round. An odd player gets a bye, which is recorded as a completed match without an opponent.
- **RoundRobin**: every player meets every other player once. The seed order is fixed when the bracket is created (`SeedNumber`), so pairs do not repeat when ratings change between rounds. The tournament finishes when all rounds are played.

## Worker

The Worker runs every 5 minutes (`ApiSettings:IntervalMinutes`). On each run it:

1. Logs in with the `service` account.
2. Gets the completed rounds from `GET /api/tournaments/rounds-completed`.
3. Recalculates Elo with `POST /api/tournaments/{id}/ratings/recalculate`.
4. Creates the next round with `POST /api/tournaments/{id}/rounds/next`.

A tournament stays in the list until the next round is created. If a run fails after the rating recalculation, the tournament is processed again on the next run. An error in one tournament does not stop the others.

## Real-time updates

- Endpoint: `ws://localhost:5000/ws/tournament`
- Authentication: the JWT is passed in the `access_token` query parameter.
- Join a tournament group with `JoinTournament(tournamentId)` and leave it with `LeaveTournament(tournamentId)`.
- Event sent after a result is saved: `MatchResultSaved`.
- Planned: `RoundCreated`.

## API endpoints

### Auth

| Method | Endpoint | Description | Access |
|---|---|---|---|
| `POST` | `/api/auth/register` | Register a player. Body: `username`, `displayName` (3–20 characters, unique), `email`, `password` | Public |
| `POST` | `/api/auth/login` | Log in and receive a JWT | Public |
| `GET` | `/api/auth/me` | Current user | Authenticated |

### Users

| Method | Endpoint | Description | Access |
|---|---|---|---|
| `GET` | `/api/users` | All users | Admin |
| `PUT` | `/api/users/{id}/role` | Change a user's role | Admin |

### Tournaments

| Method | Endpoint | Description | Access |
|---|---|---|---|
| `GET` | `/api/tournaments` | List with `search`, `status`, `discipline`, `sortBy`, `page`, `pageSize` | Public |
| `GET` | `/api/tournaments/{id}` | Tournament details with registrations and matches | Public |
| `POST` | `/api/tournaments` | Create a tournament | Organizer, Admin |
| `PUT` | `/api/tournaments/{id}` | Update a tournament | Organizer, Admin |
| `DELETE` | `/api/tournaments/{id}` | Delete a tournament | Organizer, Admin |
| `POST` | `/api/tournaments/{id}/registrations` | Register the current player | Player, Organizer, Admin |
| `POST` | `/api/tournaments/{id}/bracket` | Generate the bracket and round 1 | Organizer, Admin |
| `POST` | `/api/tournaments/{id}/rounds/next` | Create the next round or finish | Service, Organizer, Admin |
| `GET` | `/api/tournaments/rounds-completed` | Tournaments waiting for Elo recalculation | Service, Admin |
| `POST` | `/api/tournaments/{id}/ratings/recalculate` | Recalculate Elo for completed matches | Service, Organizer, Admin |

### Matches

| Method | Endpoint | Description | Access |
|---|---|---|---|
| `PUT` | `/api/matches/{id}/result` | Save a result and check whether the round is complete | Organizer, Admin |

### Players

| Method | Endpoint | Description | Access |
|---|---|---|---|
| `GET` | `/api/players/{id}/ratings` | Current rating in each game | Public |
| `GET` | `/api/players/{id}/rating-history` | Rating history in chronological order | Public |

The full description of every endpoint, with request and response examples, is in Swagger at `http://localhost:5000/swagger` (Development mode).

## Authentication in Swagger

1. Call `POST /api/auth/login` with one of the test accounts, for example `organizer` / `Organizer123!`.
2. Copy the `token` value from the response.
3. Click **Authorize** at the top right, paste the token and confirm.

## Tests

```bash
dotnet test
```

The unit tests cover:
- Elo calculation
- Bracket generation and seeding
- Round robin pairings (every pair meets exactly once)
- Detection of completed rounds for the Worker
- Match result handling

There are no integration tests with a real database yet.

## Roadmap

- Save the tournament winner and show it on the tournament page
- Allow an Organizer to change only their own tournaments
- Move secrets (JWT key, service password) out of the source code
- Send the `RoundCreated` WebSocket event
- Import completed tournaments from an external service (Lichess)
- New frontend design with game tabs, filters in the URL and player profiles
