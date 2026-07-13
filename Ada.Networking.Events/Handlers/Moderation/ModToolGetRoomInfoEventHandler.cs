using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Players;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Moderation;

namespace Ada.Networking.Events.Handlers.Moderation;

[PacketId(EventHandlerId.ModToolsRoomInfo)]
public class ModToolGetRoomInfoEventHandler(IPlayerRepository playerRepository) : INetworkPacketEventHandler
{
    public async Task HandleAsync(INetworkClient client)
    {
        if (client.Player == null || 
            client.RoomUser == null || 
            !client.Player.HasPermission(PlayerPermissionName.Moderator))
        {
            return;
        }

        var room = client.RoomUser.Room;

        await client.WriteToStreamAsync(new ModToolRoomInfoWriter
        {
            Id = room.Room.Id,
            UserCount = room.UserRepository.Count,
            OwnerInRoom = room.UserRepository.TryGetById(room.Room.OwnerId, out _),
            OwnerId = room.Room.OwnerId,
            OwnerName = await playerRepository.GetPlayerUsernameByIdAsync(room.Room.OwnerId) ?? "Unknown User",
            Unknown1 = true,
            Name = room.Room.Name,
            Description = room.Room.Description,
            Tags = room
                .Room.Tags
                .Select(t => t.Name)
                .ToList()
        });
    }
}