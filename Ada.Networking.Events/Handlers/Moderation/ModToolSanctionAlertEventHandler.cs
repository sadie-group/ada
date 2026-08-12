using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Players;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Players;

namespace Ada.Networking.Events.Handlers.Moderation;

[PacketId(EventHandlerId.ModToolsSanctionAlert)]
public class ModToolSanctionAlertEventHandler(IPlayerRepository playerRepository) : INetworkPacketEventHandler
{
    public int UserId { get; set; }
    public string Message { get; set; } = "";
    public int CfhTopic { get; set; }

    public async Task HandleAsync(INetworkClient client)
    {
        if (client.Player == null || !client.Player.HasPermission(PlayerPermissionName.Moderator))
        {
            return;
        }

        var target = playerRepository.GetPlayerLogicById(UserId);

        if (target?.NetworkObject == null)
        {
            return;
        }

        await target.NetworkObject.WriteToStreamAsync(new PlayerAlertWriter
        {
            Message = Message
        });
    }
}
