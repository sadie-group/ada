using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Networking.Writers.Camera;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Camera;

[PacketId(EventHandlerId.CameraPublishPhoto)]
public class CameraPublishPhotoEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory) : INetworkPacketEventHandler, IRunsOutsideRoomLock
{
    public required string Url { get; init; }

    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null)
        {
            return;
        }

        var playerId = player.Player.Id;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var updated = await dbContext.PlayerPhotos
            .Where(x => x.PlayerId == playerId && x.Url == Url && !x.IsPublished)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsPublished, true));

        if (updated == 0)
        {
            return;
        }

        await client.WriteToStreamAsync(new CameraPhotoPreviewWriter
        {
            Url = Url
        });
    }
}
