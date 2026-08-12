using Ada.API.Interfaces.Game.Moderation;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Players;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Moderation;

namespace Ada.Networking.Events.Handlers.Moderation;

[PacketId(EventHandlerId.ModToolsIssueDefaultSanction)]
public class ModToolIssueDefaultSanctionEventHandler(
    IModToolRepository modToolRepository) : INetworkPacketEventHandler, IRunsOutsideRoomLock
{
    public int UserId { get; set; }

    public async Task HandleAsync(INetworkClient client)
    {
        if (client.Player == null || !client.Player.HasPermission(PlayerPermissionName.Moderator))
        {
            return;
        }

        var priorSanctions = await modToolRepository.GetPriorSanctionCountAsync(UserId);

        await client.WriteToStreamAsync(new ModToolIssueInfoWriter
        {
            UserId = UserId,
            SanctionCount = priorSanctions,
            SuggestedSanction = priorSanctions switch
            {
                0 => "mute",
                1 => "mute",
                2 => "tradelock",
                _ => "ban"
            }
        });
    }
}
