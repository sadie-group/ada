namespace Ada.API.DTOs.Players;

public record PlayerSubscriptionDto
{
    public int Id { get; init; }
    public long PlayerId { get; init; }
    public PlayerDto? Player { get; init; }
    public int SubscriptionId { get; init; }
    public SubscriptionDto? Subscription { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}