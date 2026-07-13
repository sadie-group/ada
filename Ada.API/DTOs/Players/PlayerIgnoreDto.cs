namespace Ada.API.DTOs.Players;

public record PlayerIgnoreDto
{
    public int Id { get; init; }
    public long PlayerId { get; init; }
    public long TargetPlayerId { get; init; }
}