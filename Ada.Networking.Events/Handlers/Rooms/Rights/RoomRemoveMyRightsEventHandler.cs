using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Rooms;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Networking.Writers.Rooms;
using Ada.Networking.Writers.Rooms.Rights;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Rooms.Rights;

[PacketId(EventHandlerId.RoomRemoveMyRights)]
public class RoomRemoveMyRightsEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomRepository roomRepository) : INetworkPacketEventHandler, IDefersPersistence
{
    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null)
        {
            return;
        }

        var room = roomRepository.TryGetRoomById(player.State.CurrentRoomId);

        if (room == null || room.Room.OwnerId == player.Player.Id)
        {
            return;
        }

        var playerId = player.Player.Id;
        var right = room.Room.PlayerRights.FirstOrDefault(x => x.PlayerId == playerId);

        if (right == null)
        {
            return;
        }

        room.Room.PlayerRights.Remove(right);

        if (room.UserRepository.TryGetById((int) playerId, out var roomUser))
        {
            roomUser!.ControllerLevel = RoomControllerLevel.None;
            roomUser.ApplyFlatCtrlStatus();
        }

        await client.WriteToStreamAsync(new RoomRightsWriter
        {
            ControllerLevel = (int) RoomControllerLevel.None
        });

        await client.WriteToStreamAsync(new RoomRightsRevokedWriter());

        await room.BroadcastDataAsync(new RoomRemoveUserRightsWriter
        {
            RoomId = room.Room.Id,
            PlayerId = playerId
        });

        var roomId = room.Room.Id;

        _persist = async () =>
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            await dbContext.RoomPlayerRights
                .Where(x => x.RoomId == roomId && x.PlayerId == playerId)
                .ExecuteDeleteAsync();
        };
    }

    private Func<Task>? _persist;

    public Task PersistAsync() => _persist?.Invoke() ?? Task.CompletedTask;
}
