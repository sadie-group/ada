namespace Ada.Db.Models.Moderation;

public class ModerationAuditEntry
{
    public long Id { get; init; }
    public long ModeratorId { get; init; }
    public required string ModeratorUsername { get; init; }
    public long? TargetPlayerId { get; init; }
    public int? TargetRoomId { get; init; }
    public required string Action { get; init; }
    public string? Reason { get; init; }
    public required DateTime CreatedAt { get; init; }
}
