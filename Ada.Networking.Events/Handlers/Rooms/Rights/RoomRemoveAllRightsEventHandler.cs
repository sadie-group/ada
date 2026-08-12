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

[PacketId(EventHandlerId.RoomRemoveAllRights)]
public class RoomRemoveAllRightsEventHandler(
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

        if (room == null || room.Room.OwnerId != player.Player.Id)
        {
            return;
        }

        var rights = room.Room.PlayerRights.ToList();

        if (rights.Count == 0)
        {
            return;
        }

        foreach (var right in rights)
        {
            if (room.UserRepository.TryGetById((int) right.PlayerId, out var roomUser))
            {
                roomUser!.ControllerLevel = RoomControllerLevel.None;
                roomUser.ApplyFlatCtrlStatus();

                await roomUser.NetworkObject.WriteToStreamAsync(new RoomRightsWriter
                {
                    ControllerLevel = (int) RoomControllerLevel.None
                });
            }

            await room.BroadcastDataAsync(new RoomRemoveUserRightsWriter
            {
                RoomId = room.Room.Id,
                PlayerId = right.PlayerId
            });

            room.Room.PlayerRights.Remove(right);
        }

        var roomId = room.Room.Id;

        _persist = async () =>
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            await dbContext.RoomPlayerRights
                .Where(x => x.RoomId == roomId)
                .ExecuteDeleteAsync();
        };
    }

    private Func<Task>? _persist;

    public Task PersistAsync() => _persist?.Invoke() ?? Task.CompletedTask;
}
