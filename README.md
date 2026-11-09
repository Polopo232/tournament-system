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
```

## Requirements

- .NET 10 SDK

## Build and run

```bash
dotnet build
dotnet run --project src/TournamentSystem.Api
dotnet run --project src/TournamentSystem.Worker
dotnet test
```
