using Ada.API.Interfaces.Game.Moderation;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Players;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Moderation;

namespace Ada.Networking.Events.Handlers.Moderation;

[PacketId(EventHandlerId.ModToolsUserChatLog)]
public class ModToolGetUserChatLogEventHandler(IModToolRepository modToolRepository)
    : INetworkPacketEventHandler
{
    public int UserId { get; set; }

    public async Task HandleAsync(INetworkClient client)
    {
        if (client.Player == null || !client.Player.HasPermission(PlayerPermissionName.Moderator))
        {
            return;
        }

        var (username, rooms) = await modToolRepository.GetUserChatlogAsync(UserId, 150);

        await client.WriteToStreamAsync(new ModToolUserChatLogWriter
        {
            UserId = UserId,
            Username = username,
            Rooms = rooms
        });
    }
}
