using Ada.API.Interfaces.Game.Players.Friendships;
using Ada.API;

namespace Ada.API.DTOs.Players.Friendships;

public class PerkData(string? code, string? errorMessage, bool allowed) : IPerkData
{
    public string? Code { get; set; } = code;
    public string? ErrorMessage { get; set; } = errorMessage;
    public bool Allowed { get; set; } = allowed;
}