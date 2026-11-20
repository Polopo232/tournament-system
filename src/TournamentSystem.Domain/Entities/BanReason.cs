namespace TournamentSystem.Domain.Entities;

/// <summary>Konto blokeerimise põhjus.</summary>
public enum BanReason
{
    Cheating,
    Smurfing,
    BanEvasion,
    StreamSniping,
    MatchIntegrity,
    SevereAbuse
}