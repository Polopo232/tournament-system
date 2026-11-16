using System;
using System.Collections.Generic;
using System.Text;

namespace TournamentSystem.Domain.Entities;

public class PlayerRating
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Game { get; set; } = string.Empty;
    public int Rating { get; set; } = 1200;
}