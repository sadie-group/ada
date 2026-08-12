using Ada.API.Interfaces.Game.Moderation;
using Ada.Db;
using Ada.Db.Models.Moderation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ada.Game.Moderation;

public class ModerationAuditService(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    ILogger<ModerationAuditService> logger) : IModerationAuditService
{
    private const int _maxReasonLength = 512;

    public async Task RecordAsync(
        long moderatorId,
        string moderatorUsername,
        string action,
        long? targetPlayerId = null,
        int? targetRoomId = null,
        string? reason = null)
    {
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            dbContext.ModerationAuditEntries.Add(new ModerationAuditEntry
            {
                ModeratorId = moderatorId,
                ModeratorUsername = moderatorUsername,
                TargetPlayerId = targetPlayerId,
                TargetRoomId = targetRoomId,
                Action = action,
                Reason = reason?.Length > _maxReasonLength ? reason[.._maxReasonLength] : reason,
                CreatedAt = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            logger.LogError(e,
                "Failed to record moderation action {Action} by {ModeratorId}", action, moderatorId);
        }
    }
}
