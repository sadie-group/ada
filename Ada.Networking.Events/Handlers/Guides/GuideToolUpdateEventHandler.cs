using Ada.API.Interfaces.Game.Guides;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Players;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Guides;

namespace Ada.Networking.Events.Handlers.Guides;

[PacketId(EventHandlerId.GuideToolUpdate)]
public class GuideToolUpdateEventHandler(IGuideSessionService sessionService) : INetworkPacketEventHandler
{
    public bool OnDuty { get; set; }
    public bool TourRequests { get; set; }
    public bool HelperRequests { get; set; }
    public bool BullyReports { get; set; }

    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null || !player.HasPermission(PlayerPermissionName.GuideUseTool))
        {
            return;
        }

        var playerId = player.Player.Id;

        if (!OnDuty)
        {
            sessionService.SetOnDuty(playerId, false, false);
            sessionService.SetOnDuty(playerId, false, true);
        }
        else
        {
            if (HelperRequests && player.HasPermission(PlayerPermissionName.GuideGiveTours))
            {
                sessionService.SetOnDuty(playerId, true, false);
            }

            if (BullyReports && player.HasPermission(PlayerPermissionName.GuideJudgeChatReviews))
            {
                sessionService.SetOnDuty(playerId, true, true);
            }
        }

        await client.WriteToStreamAsync(new GuideToolsWriter
        {
            OnDuty = sessionService.IsOnDuty(playerId),
            GuidesOnDuty = 0,
            HelpersOnDuty = sessionService.GuidesOnDuty,
            GuardiansOnDuty = sessionService.GuardiansOnDuty
        });
    }
}
