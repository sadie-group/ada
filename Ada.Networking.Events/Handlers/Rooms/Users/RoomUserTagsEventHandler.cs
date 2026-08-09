using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Game.Rooms;
using Ada.Networking.Writers.Rooms.Users;

namespace Ada.Networking.Events.Handlers.Rooms.Users;

[PacketId(EventHandlerId.RoomUserTags)]
public class RoomUserTagsEventHandler(IRoomRepository roomRepository) : INetworkPacketEventHandler
{
    public int UserId { get; init; }

    public async Task HandleAsync(INetworkClient client)
    {
        if (client.Player == null)
        {
            return;
        }

        if (!RoomContextResolver.TryResolveRoomObjectsForClient(roomRepository, client, out var room, out _))
        {
            return;
        }

        if (room.UserRepository.TryGetById(UserId, out var specialUser) && specialUser?.NetworkObject != null)
        {
            await specialUser.NetworkObject.WriteToStreamAsync(new RoomUserTagsWriter
            {
                UserId = specialUser.Player.Player.Id,
                Tags = specialUser.Player.Player.Tags.Select(x => x.Name ?? string.Empty).ToList()
            });
        }
    }
}