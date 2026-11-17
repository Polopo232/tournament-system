namespace TournamentSystem.Domain.Entities;

public enum UserRole
{
    Admin,
    Organizer,
    Player,
    Service
}

public enum TournamentFormat
{
    SingleElimination,
    RoundRobin,
    Arena
}

public enum TournamentStatus
{
    Registration,
    InProgress,
    Finished
}

public enum MatchStatus
{
    Pending,
    Completed
}
