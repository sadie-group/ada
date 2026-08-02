using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Rooms.Users;

namespace Ada.Networking.Events.Handlers.Rooms.Furniture;

[PacketId(EventHandlerId.RoomBannedUsers)]
public class RoomBannedUsersEventHandler(IRoomRepository roomRepository) : INetworkPacketEventHandler
{
    public required int RoomId { get; init; }
    public async Task HandleAsync(INetworkClient client)
    {
        var room = roomRepository.TryGetRoomById(RoomId);

        if (room == null)
        {
            return;
        }

        var banListMap = new Dictionary<long, string>();

        foreach (var i in room.Room.PlayerBans.Where(x => x.ExpiresAt > DateTime.Now))
        {
            banListMap[i.PlayerId] = i.Player?.Username ?? string.Empty;
        }
        
        await client.WriteToStreamAsync(new RoomBannedUsersWriter
        {
            RoomId = room.Room.Id,
            BannedUsersMap = banListMap
        });
    }
}