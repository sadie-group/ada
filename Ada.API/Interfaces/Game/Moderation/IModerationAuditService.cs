namespace Ada.API.Interfaces.Game.Moderation;

public interface IModerationAuditService
{
    Task RecordAsync(
        long moderatorId,
        string moderatorUsername,
        string action,
        long? targetPlayerId = null,
        int? targetRoomId = null,
        string? reason = null);
}
