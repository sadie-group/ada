using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Networking.Writers.Camera;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Camera;

[PacketId(EventHandlerId.CameraPurchasePhoto)]
public class CameraPurchasePhotoEventHandler(
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

        if (!await dbContext.PlayerPhotos.AnyAsync(x => x.PlayerId == playerId && x.Url == Url))
        {
            return;
        }

        await client.WriteToStreamAsync(new CameraPhotoPurchaseOkWriter());
    }
}
