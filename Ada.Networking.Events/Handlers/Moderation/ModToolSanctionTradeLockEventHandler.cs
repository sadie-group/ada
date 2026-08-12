using Ada.API.Interfaces.Game.Moderation;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Players;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Players;

namespace Ada.Networking.Events.Handlers.Moderation;

[PacketId(EventHandlerId.ModToolsSanctionTradeLock)]
public class ModToolSanctionTradeLockEventHandler(
    IModToolRepository modToolRepository,
    IPlayerRepository playerRepository,
    IModerationAuditService moderationAuditService)
: INetworkPacketEventHandler, IRunsOutsideRoomLock
{
    private const int _defaultTradeLockDays = 1;

    public int UserId { get; set; }
    public string Message { get; set; } = "";
    public int CfhTopic { get; set; }

    public async Task HandleAsync(INetworkClient client)
    {
        if (client.Player == null || !client.Player.HasPermission(PlayerPermissionName.Moderator))
        {
            return;
        }

        var expiresAt = SanctionEscalation.TradeLockExpiryFor(
            await modToolRepository.GetPriorSanctionCountAsync(UserId), _defaultTradeLockDays);

        if (!await modToolRepository.ApplyTradeLockAsync(UserId, expiresAt))
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
            target.Player.Data.TradeLockExpiresAt = expiresAt;
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
            "trade-lock",
            UserId,
            null,
            Message);
    }
}
