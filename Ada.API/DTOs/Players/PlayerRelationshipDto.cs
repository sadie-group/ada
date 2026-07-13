namespace Ada.API.DTOs.Players;

public record PlayerRelationshipDto
{
    public int Id { get; init; }
    public long OriginPlayerId { get; init; }
    public PlayerDto? OriginPlayer { get; init; }
    public long TargetPlayerId { get; init; }
    public PlayerDto? TargetPlayer { get; init; }
    public int TypeId { get; set; }
}
