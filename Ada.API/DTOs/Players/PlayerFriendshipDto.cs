using Ada.Core.Enums.Game.Players;

namespace Ada.API.DTOs.Players;

public record PlayerFriendshipDto
{
    public int Id { get; set; }
    public long OriginPlayerId { get; set; }
    public PlayerDto? OriginPlayer { get; init; }
    public long TargetPlayerId { get; set; }
    public PlayerDto? TargetPlayer { get; init; }
    public PlayerFriendshipStatus  Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}