using Ada.API.Interfaces.Game.Moderation;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Players;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Events.Handlers.Moderation;

[PacketId(EventHandlerId.ModToolsSanctionBan)]
public class ModToolSanctionBanEventHandler(
    IModToolRepository modToolRepository,
    IPlayerRepository playerRepository,
    IModerationAuditService moderationAuditService)
: INetworkPacketEventHandler
{
    public int UserId { get; set; }
    public string Message { get; set; } = "";
    public int CfhTopic { get; set; }
    public int BanType { get; set; }
    public bool Unknown { get; set; }

    public async Task HandleAsync(INetworkClient client)
    {
        if (client.Player == null || !client.Player.HasPermission(PlayerPermissionName.Moderator))
        {
            return;
        }

        var expiresAt = ExpiryFor(BanType);
        var created = await modToolRepository.CreateBanAsync(
            client.Player.Player.Id, UserId, Message, expiresAt);

        if (!created)
        {
            return;
        }

        var target = playerRepository.GetPlayerLogicById(UserId);

        if (target?.NetworkObject is { } networkObject)
        {
            networkObject.WebSocket.Abort();
        }
    
        await moderationAuditService.RecordAsync(
            client.Player!.Player.Id,
            client.Player.Player.Username,
            "ban",
            UserId,
            null,
            Message);
}

    private static DateTimeOffset? ExpiryFor(int banType)
    {
        var now = DateTimeOffset.UtcNow;

        return banType switch
        {
            3 => now.AddHours(18),
            4 => now.AddDays(7),
            5 or 7 => now.AddDays(30),
            6 or 106 => null,
            _ => now.AddHours(18)
        };
    }
}
