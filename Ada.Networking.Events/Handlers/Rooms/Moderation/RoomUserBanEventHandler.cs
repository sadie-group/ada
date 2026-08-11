using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db.Models.Players;
using Ada.Db;
using Ada.Game.Rooms;
using Ada.Networking.Events.Attributes;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Rooms.Moderation;

[PacketId(EventHandlerId.RoomUserBan)]
public class RoomUserBanEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomRepository roomRepository) : INetworkPacketEventHandler, IDefersPersistence
{
    public int UserId { get; init; }
    public int RoomId { get; init; }
    public required string Length { get; init; }

    private static readonly TimeSpan _hourBan = TimeSpan.FromHours(1);
    private static readonly TimeSpan _dayBan = TimeSpan.FromDays(1);
    private static readonly TimeSpan _permanentBan = TimeSpan.FromDays(3650);

    private Func<Task>? _persist;

    [RequiresRoomRights]
    public async Task HandleAsync(INetworkClient client)
    {
        if (!RoomContextResolver.TryResolveRoomObjectsForClient(roomRepository, client, out var room, out var actor))
        {
            return;
        }

        if (room.Room.Id != RoomId)
        {
            return;
        }

        if (!RoomModerationRules.CanActOn(room, actor, UserId, out _))
        {
            return;
        }

        var expiresAt = DateTimeOffset.UtcNow.Add(Length switch
        {
            "RWUAM_BAN_USER_HOUR" => _hourBan,
            "RWUAM_BAN_USER_DAY" => _dayBan,
            "RWUAM_BAN_USER_PERM" => _permanentBan,
            _ => _hourBan
        });

        room.Room.PlayerBans.Add(new PlayerRoomBanDto
        {
            PlayerId = UserId,
            RoomId = room.Room.Id,
            ExpiresAt = expiresAt
        });

        await room.UserRepository.TryRemoveAsync(UserId, notifyLeft: true, hotelView: true);

        var roomId = room.Room.Id;
        var playerId = (long) UserId;

        _persist = async () =>
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            await dbContext.Set<PlayerRoomBan>()
                .Where(x => x.RoomId == roomId && x.PlayerId == playerId)
                .ExecuteDeleteAsync();

            dbContext.Set<PlayerRoomBan>().Add(new PlayerRoomBan
            {
                PlayerId = playerId,
                RoomId = roomId,
                ExpiresAt = expiresAt
            });

            await dbContext.SaveChangesAsync();
        };
    }

    public Task PersistAsync() => _persist?.Invoke() ?? Task.CompletedTask;
}
