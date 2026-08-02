using Ada.API;
using Ada.API.Interfaces.Game.Moderation;
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

public sealed class PlayerLoginPacketService(IModerationTicketService ticketService)
{
    public async Task SendAsync(INetworkObject client, IPlayerLogic playerLogic)
    {
        var player = playerLogic.Player;
        var data = player.Data;

        await client.WriteToStreamAsync(new NoobnessLevelWriter
        {
            Level = 1
        });

        if (data?.HomeRoomId != null)
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
            Club = player.Subscriptions.Any(x => x.Subscription?.Name == "HABBO_CLUB") ? 2 : 0,
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
            ShowNotifications = player.GameSettings?.ShowNotifications ?? true
        });

        await client.WriteToStreamAsync(new PlayerAchievementScoreWriter
        {
            AchievementScore = data?.AchievementScore ?? 0
        });

        if (playerLogic.HasPermission(PlayerPermissionName.Moderator))
        {
            await client.WriteToStreamAsync(new ModToolsWriter
            {
                Issues = ticketService.GetActiveTickets().Select(x => new IssueData
                {
                    IssueId = x.Id,
                    State = (int) x.State,
                    CategoryId = x.CategoryId,
                    ReportedCategoryId = 0,
                    IssueAgeInMs = (int) Math.Clamp((DateTimeOffset.UtcNow - x.CreatedAt).TotalMilliseconds, 0, int.MaxValue),
                    Priority = 1,
                    GroupingId = 0,
                    ReporterUserId = (int) x.ReporterPlayerId,
                    ReporterUsername = x.ReporterUsername,
                    ReportedUserId = (int) (x.ReportedPlayerId ?? 0),
                    ReportedUsername = x.ReportedUsername,
                    PickerUserId = (int) (x.PickedByPlayerId ?? 0),
                    PickerUsername = x.PickedByUsername,
                    Message = x.Message,
                    ChatRecordId = -1,
                    Patterns = []
                }).ToList(),
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
