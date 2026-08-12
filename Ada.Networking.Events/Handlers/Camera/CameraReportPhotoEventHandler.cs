using Ada.API.Interfaces.Game.Moderation;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Camera;

[PacketId(EventHandlerId.CameraReportPhoto)]
public class CameraReportPhotoEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IModerationAuditService moderationAuditService)
    : INetworkPacketEventHandler, IRunsOutsideRoomLock
{
    public required string Url { get; init; }

    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null)
        {
            return;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var photo = await dbContext.PlayerPhotos.FirstOrDefaultAsync(x => x.Url == Url && x.IsPublished);

        if (photo == null)
        {
            return;
        }

        photo.ReportCount++;

        await dbContext.SaveChangesAsync();

        await moderationAuditService.RecordAsync(
            player.Player.Id,
            player.Player.Username,
            "photo-report",
            photo.PlayerId,
            photo.RoomId,
            Url);
    }
}
