using Ada.API.Interfaces.Game.Moderation;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Players;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Players;

namespace Ada.Networking.Events.Handlers.Moderation;

[PacketId(EventHandlerId.ModToolsSanctionMute)]
public class ModToolSanctionMuteEventHandler(
    IModToolRepository modToolRepository,
    IPlayerRepository playerRepository,
    IModerationAuditService moderationAuditService)
: INetworkPacketEventHandler, IRunsOutsideRoomLock
{
    private const int _defaultMuteHours = 1;

    public int UserId { get; set; }
    public string Message { get; set; } = "";
    public int CfhTopic { get; set; }

    public async Task HandleAsync(INetworkClient client)
    {
        if (client.Player == null || !client.Player.HasPermission(PlayerPermissionName.Moderator))
        {
            return;
        }

        var expiresAt = SanctionEscalation.MuteExpiryFor(
            await modToolRepository.GetPriorSanctionCountAsync(UserId), _defaultMuteHours);

        if (!await modToolRepository.ApplyMuteAsync(UserId, expiresAt))
        {
            return;
        }

        var target = playerRepository.GetPlayerLogicById(UserId);

        if (target == null)
        {
            return;
        }

        if (target.Player.Data != null)
        {
            target.Player.Data.MuteExpiresAt = expiresAt;
        }

        if (!string.IsNullOrWhiteSpace(Message) && target.NetworkObject != null)
        {
            await target.NetworkObject.WriteToStreamAsync(new PlayerAlertWriter
            {
                Message = Message
            });
        }
    
        await moderationAuditService.RecordAsync(
            client.Player!.Player.Id,
            client.Player.Player.Username,
            "mute",
            UserId,
            null,
            Message);
}
}
