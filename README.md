# Tournament System

A system for organizing esports, chess and table tennis tournaments.
Organizers create tournaments, players register, brackets and pairings are generated automatically,
and every player has an Elo rating that changes after each match.

## Project structure

```
TournamentSystem.sln
src/
  TournamentSystem.Domain/          Entities and enums
  TournamentSystem.Application/     Business logic (services, DTOs, interfaces)
  TournamentSystem.Infrastructure/  EF Core DbContext, migrations, repositories
  TournamentSystem.Api/             ASP.NET Core Web API (controllers, JWT, Swagger)
  TournamentSystem.Worker/          Worker Service (recalculates Elo and creates next rounds)
tests/
  TournamentSystem.UnitTests/       xUnit unit tests
tournament-web/                     React web client (Vite)
```

## Requirements

- .NET 10 SDK
- Node.js 20+

## Database

SQL Server LocalDB is used by default (connection string in `src/TournamentSystem.Api/appsettings.json`).

```bash
dotnet tool restore
dotnet ef database update --project src/TournamentSystem.Infrastructure --startup-project src/TournamentSystem.Api
```

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
