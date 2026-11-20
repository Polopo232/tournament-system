# Tournament System

A system for organizing esports, chess and table tennis tournaments.
Organizers create tournaments, players register with a nickname, brackets and pairings are generated automatically,
and every player has a separate Elo rating for each game that changes after each match.
Chess arenas from Lichess are imported and synchronized automatically, including live standings and ongoing games.

## Project structure

```text
TournamentSystem.sln
src/
  TournamentSystem.Domain/          Entities and enums (users, tournaments, matches, ratings, ban reasons)
  TournamentSystem.Application/     Business logic (services, DTOs, interfaces, exceptions, security and ban rules)
  TournamentSystem.Infrastructure/  EF Core DbContext, migrations, repositories, seed data, Lichess client
  TournamentSystem.Api/             ASP.NET Core Web API (controllers, JWT, Swagger, SignalR, ProblemDetails, .http tests)
  TournamentSystem.Worker/          Worker Service (Elo, next rounds, Lichess synchronization)
tests/
  TournamentSystem.UnitTests/       xUnit + Moq unit tests
tournament-web/                     React web client (Vite)
```

## Features

- **Tournament CRUD and querying:** search, filters (`status`, `discipline`, `format`), sorting (`sortBy`), pagination (`page`, `pageSize`).
- **Player nicknames:** every player has a login (`Username`) and a public nickname (`DisplayName`, unique, 3–20 characters). Nicknames are shown in brackets, matches, registrations and the leaderboard.
- **Three tournament formats:**
  - `SingleElimination`: no draws, byes for odd player counts, the winner is the last remaining player.
  - `RoundRobin`: every player meets every other player once. Pairings do not repeat between rounds.
  - `Arena`: Lichess arenas. Live standings and live games, no brackets (imported, not created here).
- **Seeding by Elo:** players are seeded by their rating in the tournament's game. The seed order is stored (`SeedNumber`) and used for all later rounds.
- **Separate Elo per game:** a player has one rating for each game (for example `Chess`, `CS2`, `Dota2`, `TableTennis`). Rating history is stored with the game name and the tournament.
- **Manual finish:** an Organizer or Admin can finish a tournament when all matches of the final stage are completed. Elo is recalculated first, then the winner is saved.
- **Automatic finish:** the Worker creates the next round, or finishes the tournament when no next round is needed.
- **Winner:** saved when the tournament finishes and shown on the tournament page and in the player's profile.
- **Ownership rules:** an Organizer can change only their own tournaments, their brackets, their match results and finish them. Admin and Service can act on any tournament.
- **Lichess arenas:** finished and running `rapid` arenas with a 10-minute clock are synchronized automatically. Standings are updated on each cycle, the winner is set when the arena ends. Lichess ratings are stored separately under `Chess (rapid)` and do not affect the Elo of regular tournaments.
- **Live games from Lichess:** a running arena shows its current games with a link to Lichess and an embedded viewer in a modal window.
- **User management (Admin):** search by name, login or e-mail, filters by role and source (local or Lichess), sorting and paging, role changes.
- **Account blocking (Admin):** an account can be blocked for a reason (Cheating, Smurfing, Ban evasion, Stream sniping, Match integrity, Severe abuse) and for a duration (1 day, 3 days, 1 week, 1 month, 1 year, 5 years or permanent). Blocked users cannot log in, and their existing tokens stop working. The block expires automatically. The player's profile shows an active ban.
- **Player profile:** public profile with nickname, ratings in every game, tournament wins, Elo chart with tooltips and a history table linked to tournaments.
- **Leaderboard:** ranking per game with nickname search and paging.
- **Background Worker:** every 5 minutes it recalculates Elo for finished rounds and creates the next round. Lichess arenas are synchronized every 60 seconds by a separate background service.
- **Real-time updates:** match results and new rounds are pushed to clients over WebSocket (SignalR).
- **Authentication and roles:** JWT Bearer tokens with `Admin`, `Organizer`, `Player` and `Service` roles.
- **Errors:** centralized `IExceptionHandler` returns RFC 7807 ProblemDetails for `400`, `401`, `403`, `404` and `500`.
- **Documentation and tests:** Swagger UI with XML comments and examples, and a `.http` file with ready-made requests.

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

## Configuration

Lichess synchronization is configured in `src/TournamentSystem.Api/appsettings.json`:

```json
"LichessSync": {
  "PerfKeys": [ "rapid" ],
  "ClockMinutes": 10
}
```

- `PerfKeys`: Lichess categories to synchronize (`bullet`, `blitz`, `rapid`, ...).
- `ClockMinutes`: time control of the arena in minutes. `0` synchronizes any time control.

The polling interval of the Lichess synchronization is set in `src/TournamentSystem.Worker/appsettings.json` as `LichessSync:IntervalSeconds` (default 60). Values below 15 seconds are not recommended, because Lichess limits the number of requests.

## Database

SQL Server LocalDB is used by default. The connection string is in `src/TournamentSystem.Api/appsettings.json`.

```cmd
dotnet tool restore
dotnet build
dotnet ef database update --project src/TournamentSystem.Infrastructure --startup-project src/TournamentSystem.Api
```

On start, the API applies migrations. In the Development environment it also fills an empty database with test data, including demo tournaments with finished matches.
Outside Development, only migrations are applied and no test accounts are created.

If the model has changed and the database no longer matches the migrations, drop the database and start the API again.
Dropping deletes all data, including Lichess imports.

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

Before building, stop the API and the Worker. Running processes keep their DLL files locked, and the build fails with `MSB3027`.

## Swagger and API usage

With the API running in Development mode, open `http://localhost:5000/swagger`.

### Authenticate in Swagger

1. Call `POST /api/auth/login` with a test account, for example `organizer` / `Organizer123!`.
2. Copy the `token` value from the response.
3. Click **Authorize** at the top right, paste the token and confirm.

### API tests with the `.http` file

The file `src/TournamentSystem.Api/TournamentSystem.Api.http` contains requests for every group of endpoints: authentication, tournaments, registrations, results, players, user management, Lichess and error cases with the expected status codes.

Usage in Visual Studio or VS Code (REST Client extension):

1. Log in with the four accounts in section 1 of the file.
2. Copy each `token` value into the matching variable at the top of the file (`@orgToken`, `@playerToken`, `@adminToken`, `@serviceToken`).
3. Run the other requests. Tokens live for 120 minutes, so log in again when requests start returning 401.

### Rating and tournament rules

- A new player starts at 1200 in every game. Elo is calculated with K = 32. A draw counts as 0.5.
- The rating used for seeding and for updates is the rating in the tournament's game.
- Lichess arena ratings are stored under `Chess (<category>)`, for example `Chess (rapid)`. They are copied from Lichess and do not change the Elo of other tournaments.
- In a single elimination tournament a draw is not allowed when a result is submitted.
- In a round robin tournament, a win gives 1 point and a draw gives 0.5 points. Byes give no points. The winner is the player with the most points. On equal points, the better seed wins.
- A tournament can be finished manually only when all matches are completed and the final stage is reached: the single final match in an elimination tournament, or the last round in a round robin tournament.

### Ban reasons and durations

| Value | Meaning |
|---|---|
| `Cheating` | Prohibited software, cheats or exploits |
| `Smurfing` | Playing on a second account below one's skill level |
| `BanEvasion` | Playing on another account while banned |
| `StreamSniping` | Watching an opponent's live stream during a match |
| `MatchIntegrity` | Bug abuse, rank manipulation or match fixing |
| `SevereAbuse` | Identity attacks, slurs, threats and content violations |

| Value | Duration |
|---|---|
| `Day1` | 1 day |
| `Days3` | 3 days |
| `Week1` | 1 week |
| `Month1` | 1 month |
| `Year1` | 1 year |
| `Years5` | 5 years |
| `Permanent` | No end date |
| `Minute1` | 1 minute, only in the Development environment, for testing |

Administrators choose the reason and the duration manually. The admin user list suggests a duration for each reason. An administrator cannot block their own account.

### Main endpoints

| Method | Endpoint | Description | Access |
|---|---|---|---|
| `POST` | `/api/auth/register` | Register a player. Body: `username`, `displayName`, `email`, `password` | Public |
| `POST` | `/api/auth/login` | Log in and receive a JWT. Blocked accounts receive 403 with the reason and expiry | Public |
| `GET` | `/api/auth/me` | Current user | Authenticated |
| `GET` | `/api/users` | Users with `search`, `role`, `source` (`local` or `lichess`), `sortBy` (`name`, `newest`, `role`), `page`, `pageSize` | Admin |
| `PUT` | `/api/users/{id}/role` | Change a user's role | Admin |
| `PUT` | `/api/users/{id}/block` | Block with `reason` and `duration`, or unblock with `isBlocked: false` | Admin |
| `GET` | `/api/tournaments` | List tournaments (`search`, `status`, `discipline`, `format`, `sortBy`, `page`, `pageSize`) | Public |
| `GET` | `/api/tournaments/{id}` | Tournament details with registrations, matches, final standings, live games and winner | Public |
| `POST` | `/api/tournaments` | Create a tournament (returns 201) | Organizer, Admin |
| `PUT` | `/api/tournaments/{id}` | Update a tournament (own tournaments only, Organizer) | Organizer, Admin |
| `DELETE` | `/api/tournaments/{id}` | Delete a tournament (own tournaments only, Organizer) | Organizer, Admin |
| `POST` | `/api/tournaments/{id}/registrations` | Register the current player (returns 201) | Player, Organizer, Admin |
| `POST` | `/api/tournaments/{id}/bracket` | Generate the bracket and round 1, seeded by Elo (returns 201) | Organizer, Admin |
| `POST` | `/api/tournaments/{id}/rounds/next` | Create the next round or finish the tournament | Service, Organizer, Admin |
| `POST` | `/api/tournaments/{id}/finish` | Finish the tournament manually and set the winner | Organizer, Admin |
| `GET` | `/api/tournaments/rounds-completed` | Tournaments with completed rounds waiting for Elo recalculation | Service, Admin |
| `POST` | `/api/tournaments/{id}/ratings/recalculate` | Recalculate Elo for completed matches and save history | Service, Organizer, Admin |
| `PUT` | `/api/matches/{id}/result` | Submit a match result (own tournaments only, Organizer) | Organizer, Admin |
| `GET` | `/api/players/{id}` | Public profile: nickname, ratings, wins and active ban | Public |
| `GET` | `/api/players/{id}/ratings` | Current rating in each game | Public |
| `GET` | `/api/players/{id}/rating-history` | Rating history in chronological order, with tournament name | Public |
| `GET` | `/api/players/leaderboard` | Leaderboard for a game (`game`, `search`, `page`, `pageSize`) | Public |
| `POST` | `/api/admin/import/lichess/{arenaId}` | Import one finished Lichess arena by its 8-character ID | Admin |
| `POST` | `/api/service/lichess/sync` | Synchronize Lichess arenas by the configured categories | Service, Admin |

Every endpoint has a description and status codes in Swagger.

## Worker

The Worker consists of two background services.

**Tournament worker** runs every 5 minutes (`ApiSettings:IntervalMinutes`). On each run it:

1. Logs in with the `service` account.
2. Gets the tournaments waiting for processing from `GET /api/tournaments/rounds-completed`.
3. Recalculates Elo with `POST /api/tournaments/{id}/ratings/recalculate`.
4. Creates the next round with `POST /api/tournaments/{id}/rounds/next`.

A tournament stays in the list until the next round exists. If a run fails, the tournament is processed again on the next run.
An error in one tournament does not stop the others.

**Lichess synchronization** runs every 60 seconds (`LichessSync:IntervalSeconds`) and calls `POST /api/service/lichess/sync`. An error in the synchronization is logged and does not stop the Worker.

## Lichess integration

- Arenas are read from `GET /api/tournament` on lichess.org. Only the categories and time controls from `LichessSync` are taken.
- A running arena is stored as a tournament with the status `InProgress`. Its standings and its current games (`duels`) are refreshed on each synchronization. The tournament page shows the games with links to Lichess and an embedded viewer.
- A finished arena gets the status `Finished` and its winner (rank 1). Its games are removed from the live list.
- A finished arena with a winner is not synchronized again.
- A running arena that has dropped out of the active list is requested separately, up to five per run, so it does not stay in progress forever.
- All requests to Lichess go through a rate limiter: at least 1.1 seconds between requests, and a retry after a `429 Too Many Requests` response.
- Players who do not exist yet are created with the login `lichess_<name>` and a random password, so they cannot log in. Their nickname is their Lichess name.
- Arenas do not have brackets: pairings and matches are not created for them.

## User management and bans

- The admin user list supports search by nickname, login or e-mail, filters by role and by source (local or Lichess), sorting and paging.
- A ban is applied from the admin list with a reason and a duration. The ban is stored with its start and its end. When the end time passes, the account becomes active again without any action from an administrator.
- A blocked user cannot log in. The login response contains the reason and the end time.
- Tokens issued before the ban are checked on every request. A blocked account's token is rejected immediately.
- Players who are banned see a notice on their public profile until the ban ends.

## Real-time updates

The API provides a SignalR endpoint at `ws://localhost:5000/ws/tournament`.
The JWT is passed in the `access_token` query parameter.

Clients join a tournament group with `JoinTournament(tournamentId)` and leave it with `LeaveTournament(tournamentId)`.

Events:

- `MatchResultSaved`: a match result was saved. The payload contains the updated match.
- `RoundCreated`: a new round was created. Clients should reload the tournament.

## Tests

```cmd
dotnet test
```

The unit tests cover:

- Elo calculation
- Bracket generation and seeding
- Round robin pairings (every pair meets exactly once)
- Detection of completed rounds for the Worker, including a failed next-round run
- Winner selection in both formats and manual finish rules
- Match result handling and the `RoundCreated` notification
- Ownership rules for tournament management
- Lichess import: filtering by category and time control, skipping finished arenas, storing category ratings, handling of arenas without a matching result
- Account bans: login is refused during an active ban, expired bans do not apply, permanent bans apply, durations are calculated correctly, administrators cannot block themselves

There are no integration tests with a real database yet.

## Frontend

The React client (`tournament-web`) has these pages:

- `/tournaments`: tournament catalogue with game tabs, status filters, search, sorting and URL-based filters
- `/tournaments/:id`: tournament details, participants, bracket, winner, round controls, live Lichess games (for arenas) and the standings table
- `/players/:id`: player profile with wins, ratings per game, Elo chart with tooltips, history linked to tournaments, and a ban notice when applicable
- `/leaderboard`: leaderboard per game with search
- `/admin/users`: user management with search, filters, role changes and bans
- `/login`, `/register`: authentication (registration requires a nickname)

## Known limitations

- Lichess arenas do not generate brackets and do not change Elo.
- The Lichess import by ID (`/api/admin/import/lichess/{arenaId}`) stores players with the default rating of 1200 in `Chess`. The automatic synchronization stores them under `Chess (rapid)`.
- Live games are refreshed by polling, not by push. The embedded viewer shows the game as Lichess provides it.
- The tournament detail page refreshes running arenas every 30 seconds.
- There are no integration tests with a real database.
- Ban durations are fixed; there is no automatic escalation for repeated offences.

## Roadmap

- Automatic tournament series: templates that create a tournament on schedule, open registration and generate the bracket at the start time
- Second client application for the API (console or Razor-based administration page)
- Integration tests with a real database
- Notifications to the player when a ban is applied or lifted
