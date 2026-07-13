using Ada.API;
using Ada.API.Interfaces.Game.Players;
using Ada.Core.Enums.Game.Players;
using Ada.Networking.Writers.Handshake;
using Ada.Networking.Writers.Moderation;
using Ada.Networking.Writers.Players;
using Ada.Networking.Writers.Players.Clothing;
using Ada.Networking.Writers.Players.Effects;
using Ada.Networking.Writers.Players.Navigator;
using Ada.Networking.Writers.Players.Other;
using Ada.Networking.Writers.Players.Permission;
using Ada.Networking.Writers.Players.Rooms;

namespace Ada.Networking.Events;

public sealed class PlayerLoginPacketService
{
    public async Task SendAsync(INetworkObject client, IPlayerLogic playerLogic)
    {
        var player = playerLogic.Player;
        var data = player.Data;

        await client.WriteToStreamAsync(new NoobnessLevelWriter
        {
            Level = 1
        });

        if (data.HomeRoomId.HasValue)
        {
            await client.WriteToStreamAsync(new PlayerHomeRoomWriter
            {
                HomeRoom = data.HomeRoomId.Value,
                RoomIdToEnter = data.HomeRoomId.Value
            });
        }

        await client.WriteToStreamAsync(new PlayerEffectListWriter
        {
            Effects = []
        });

        await client.WriteToStreamAsync(new PlayerClothingListWriter
        {
            SetIds = [],
            FurnitureNames = []
        });

        await client.WriteToStreamAsync(new PlayerPermissionsWriter
        {
            Club = player.Subscriptions.Any(x => x.Subscription.Name == "HABBO_CLUB") ? 2 : 0,
            Rank = player.Roles.Count != 0 ? player.Roles.Max(x => x.Id) : 1,
            Ambassador = true
        });

        await client.WriteToStreamAsync(new PlayerNavigatorSettingsWriter
        {
            NavigatorSettings = player.NavigatorSettings!
        });

        await client.WriteToStreamAsync(new PlayerStatusWriter
        {
            IsOpen = true,
            IsShuttingDown = false,
            IsAuthentic = true
        });

        await client.WriteToStreamAsync(new PlayerNotificationSettingsWriter
        {
            ShowNotifications = player.GameSettings.ShowNotifications
        });

        await client.WriteToStreamAsync(new PlayerAchievementScoreWriter
        {
            AchievementScore = data.AchievementScore
        });

        if (playerLogic.HasPermission(PlayerPermissionName.Moderator))
        {
            await client.WriteToStreamAsync(new ModToolsWriter
            {
                Issues = [],
                MessageTemplates = [],
                RoomMessageTemplates = [],
                Unknown3 = 0,
                CallForHelpPermission = true,
                ChatLogsPermission = true,
                AlertPermission = true,
                KickPermission = true,
                BanPermission = true,
                RoomAlertPermission = true,
                RoomKickPermission = true
            });
        }
    }
}
