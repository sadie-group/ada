using Ada.Core.Shared.Attributes;

namespace Ada.API.DTOs.Players;

public record PlayerSavedSearchDto
{
    [PacketData] public int Id { get; set; }
    [PacketData] public string? Search { get; set; }
    [PacketData] public string? Filter { get; set; }
    [PacketData] public string Localization { get; set; } = "";
    public long PlayerId { get; set; }
    
}
