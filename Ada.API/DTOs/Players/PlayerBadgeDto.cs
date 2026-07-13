namespace Ada.API.DTOs.Players;

public record PlayerBadgeDto
{
    public int Id { get; init; }
    public long PlayerId { get; init; }
    public int BadgeId { get; init; }
    public BadgeDto? Badge { get; init; }
    public int Slot { get; init; }
}