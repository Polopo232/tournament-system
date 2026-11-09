# Tournament System

A system for organizing esports, chess and table tennis tournaments.
Organizers create tournaments, players register, brackets and pairings are generated automatically,
and every player has an Elo rating that changes after each match.

## Project structure

```
TournamentSystem.sln
src/
  TournamentSystem.Api/      ASP.NET Core Web API (business logic, EF Core, JWT)
  TournamentSystem.Worker/   Worker Service (recalculates Elo and creates next rounds)
tests/
  TournamentSystem.Tests/    xUnit unit tests
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
