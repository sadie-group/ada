using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Players;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Moderation;

namespace Ada.Networking.Events.Handlers.Moderation;

[PacketId(EventHandlerId.ModToolsRoomChatLog)]
public class ModToolGetRoomChatLogEventHandler : INetworkPacketEventHandler
{
    public async Task HandleAsync(INetworkClient client)
    {
        if (client.Player == null || 
            client.RoomUser == null || 
            !client.Player.HasPermission(PlayerPermissionName.Moderator))
        {
            return;
        }

        await client.WriteToStreamAsync(new ModToolRoomChatLogWriter
        {
            Unknown1 = 1,
            Unknown2 = 2,
            Unknown3 = "roomName",
            Unknown4 = 2,
            Unknown5 = client.RoomUser.Room.Room.Name,
            Unknown6 = "roomId",
            Unknown7 = 1,
            Unknown8 = client.RoomUser.Room.Room.Id,
            Messages = client
                .RoomUser
                .Room
                .Room.ChatMessages
                .Take(150)
                .ToList()
        });
    }
}