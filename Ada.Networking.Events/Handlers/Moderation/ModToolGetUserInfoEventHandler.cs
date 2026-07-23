using Ada.API.Interfaces.Game.Moderation;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Players;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Moderation;

namespace Ada.Networking.Events.Handlers.Moderation;

[PacketId(EventHandlerId.ModToolsUserInfo)]
public class ModToolGetUserInfoEventHandler(
    IModToolRepository modToolRepository,
    IPlayerRepository playerRepository)
    : INetworkPacketEventHandler
{
    public int UserId { get; set; }

    public async Task HandleAsync(INetworkClient client)
    {
        if (client.Player == null || !client.Player.HasPermission(PlayerPermissionName.Moderator))
        {
            return;
        }

        var user = await modToolRepository.GetUserInfoAsync(UserId);

        if (user == null)
        {
            return;
        }

        var online = playerRepository.GetPlayerLogicById(UserId) != null;
        var accountAgeMinutes = (int) Math.Max(0, (DateTimeOffset.UtcNow - user.CreatedAt).TotalMinutes);

        await client.WriteToStreamAsync(new ModToolUserInfoWriter
        {
            User = user,
            Online = online,
            AccountAgeMinutes = accountAgeMinutes,
            MinutesSinceLastLogin = 0
        });
    }
}
