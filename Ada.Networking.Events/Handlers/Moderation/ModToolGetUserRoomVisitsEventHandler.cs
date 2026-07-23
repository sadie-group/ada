using Ada.API.Interfaces.Game.Moderation;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Players;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Moderation;

namespace Ada.Networking.Events.Handlers.Moderation;

[PacketId(EventHandlerId.ModToolsUserRoomVisits)]
public class ModToolGetUserRoomVisitsEventHandler(IModToolRepository modToolRepository)
    : INetworkPacketEventHandler
{
    public int UserId { get; set; }

    public async Task HandleAsync(INetworkClient client)
    {
        if (client.Player == null || !client.Player.HasPermission(PlayerPermissionName.Moderator))
        {
            return;
        }

        var (username, visits) = await modToolRepository.GetRoomVisitsAsync(UserId, 100);

        await client.WriteToStreamAsync(new ModToolUserRoomVisitsWriter
        {
            UserId = UserId,
            Username = username,
            Visits = visits
        });
    }
}
