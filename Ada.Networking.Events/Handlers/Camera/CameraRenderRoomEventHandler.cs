using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Db.Models.Players;
using Ada.Networking.Writers.Camera;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Camera;

[PacketId(EventHandlerId.CameraRenderRoom)]
public class CameraRenderRoomEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomRepository roomRepository) : INetworkPacketEventHandler, IRunsOutsideRoomLock
{
    private const int _cooldownSeconds = 5;

    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null)
        {
            return;
        }

        var room = roomRepository.TryGetRoomById(player.State.CurrentRoomId);

        if (room == null)
        {
            return;
        }

        var playerId = player.Player.Id;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var cutoff = DateTime.UtcNow.AddSeconds(-_cooldownSeconds);

        if (await dbContext.PlayerPhotos.AnyAsync(x => x.PlayerId == playerId && x.CreatedAt > cutoff))
        {
            return;
        }

        var photo = new PlayerPhoto
        {
            PlayerId = playerId,
            RoomId = room.Room.Id,
            Url = $"{playerId}-{room.Room.Id}-{DateTime.UtcNow.Ticks}.png",
            CreatedAt = DateTime.UtcNow
        };

        dbContext.PlayerPhotos.Add(photo);

        await dbContext.SaveChangesAsync();

        await client.WriteToStreamAsync(new CameraPhotoPreviewWriter
        {
            Url = photo.Url
        });
    }
}
