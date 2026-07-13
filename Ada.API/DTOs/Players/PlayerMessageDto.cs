namespace Ada.API.DTOs.Players;

public class PlayerMessageDto
{
    public int Id { get; init; }
    public long OriginPlayerId { get; init; }
    public PlayerDto? OriginPlayer { get; init; }
    public long TargetPlayerId { get; init; }
    public PlayerDto? TargetPlayer { get; init; }
    public string? Message { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}