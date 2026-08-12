using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Rooms.Settings;

[PacketId(EventHandlerId.RoomSetThumbnail)]
public class RoomSetThumbnailEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomRepository roomRepository) : INetworkPacketEventHandler, IDefersPersistence
{
    public required string Thumbnail { get; init; }

    private const int _maxThumbnailLength = 128;

    public Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null)
        {
            return Task.CompletedTask;
        }

        var room = roomRepository.TryGetRoomById(player.State.CurrentRoomId);

        if (room == null || room.Room.OwnerId != player.Player.Id)
        {
            return Task.CompletedTask;
        }

        var thumbnail = Thumbnail.Trim();

        if (thumbnail.Length > _maxThumbnailLength)
        {
            return Task.CompletedTask;
        }

        var roomId = room.Room.Id;

        _persist = async () =>
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            await dbContext.Rooms
                .Where(x => x.Id == roomId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Thumbnail, thumbnail));
        };

        return Task.CompletedTask;
    }

    private Func<Task>? _persist;

    public Task PersistAsync() => _persist?.Invoke() ?? Task.CompletedTask;
}
