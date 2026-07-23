using Microsoft.EntityFrameworkCore;
using Ada.API.DTOs.Moderation;
using Ada.API.Interfaces.Game.Moderation;
using Ada.Db;

namespace Ada.Game.Moderation;

public class ModToolRepository(IDbContextFactory<AdaDbContext> dbContextFactory) : IModToolRepository
{
    public async Task<ModToolUserInfoDto?> GetUserInfoAsync(long userId)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();

        var user = await db.Players
            .AsNoTracking()
            .Where(p => p.Id == userId)
            .Select(p => new
            {
                p.Id,
                p.Username,
                Look = p.AvatarData!.FigureCode,
                p.CreatedAt,
                p.Email,
                Role = p.Roles.OrderByDescending(r => r.Id).FirstOrDefault(),
                BanCount = db.PlayerBans.Count(b => b.PlayerId == p.Id)
            })
            .FirstOrDefaultAsync();

        if (user == null)
        {
            return null;
        }

        return new ModToolUserInfoDto
        {
            UserId = user.Id,
            Username = user.Username,
            Look = user.Look ?? "",
            CreatedAt = user.CreatedAt,
            Email = user.Email,
            RankId = user.Role?.Id ?? 0,
            RankName = user.Role?.Name ?? "User",
            BanCount = user.BanCount
        };
    }

    public async Task<(string Username, IReadOnlyList<ModToolRoomVisitDto> Visits)> GetRoomVisitsAsync(
        long userId, int limit)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();

        var username = await db.Players
            .Where(p => p.Id == userId)
            .Select(p => p.Username)
            .FirstOrDefaultAsync() ?? "";

        var visits = await db.PlayerRoomVisits
            .AsNoTracking()
            .Where(v => v.PlayerId == userId)
            .OrderByDescending(v => v.CreatedAt)
            .Take(limit)
            .Join(db.Rooms, v => v.RoomId, r => r.Id, (v, r) => new ModToolRoomVisitDto
            {
                RoomId = v.RoomId,
                RoomName = r.Name,
                EnteredAt = v.CreatedAt
            })
            .ToListAsync();

        return (username, visits);
    }

    public async Task<(string Username, IReadOnlyList<ModToolChatRoomDto> Rooms)> GetUserChatlogAsync(
        long userId, int limit)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();

        var username = await db.Players
            .Where(p => p.Id == userId)
            .Select(p => p.Username)
            .FirstOrDefaultAsync() ?? "";

        var lines = await db.RoomChatMessages
            .AsNoTracking()
            .Where(m => m.PlayerId == userId)
            .OrderByDescending(m => m.Id)
            .Take(limit)
            .Join(db.Rooms, m => m.RoomId, r => r.Id, (m, r) => new
            {
                m.RoomId,
                RoomName = r.Name,
                m.PlayerId,
                Username = m.Player!.Username,
                m.Message,
                m.CreatedAt
            })
            .ToListAsync();

        var rooms = lines
            .GroupBy(l => new { l.RoomId, l.RoomName })
            .Select(g => new ModToolChatRoomDto
            {
                RoomId = g.Key.RoomId,
                RoomName = g.Key.RoomName,
                Lines = g
                    .OrderBy(l => l.CreatedAt)
                    .Select(l => new ModToolChatLineDto
                    {
                        CreatedAt = l.CreatedAt,
                        PlayerId = l.PlayerId,
                        Username = l.Username,
                        Message = l.Message ?? ""
                    })
                    .ToList()
            })
            .ToList();

        return (username, rooms);
    }

    public async Task<bool> CreateBanAsync(long moderatorId, long targetId, string reason, DateTimeOffset? expiresAt)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();

        var moderator = await db.Players.FirstOrDefaultAsync(p => p.Id == moderatorId);
        var target = await db.Players.FirstOrDefaultAsync(p => p.Id == targetId);

        if (moderator == null || target == null)
        {
            return false;
        }

        db.PlayerBans.Add(new Db.Models.Players.PlayerBan
        {
            CreatorId = moderatorId,
            Creator = moderator,
            PlayerId = targetId,
            Player = target,
            Reason = reason,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = expiresAt
        });

        await db.SaveChangesAsync();
        return true;
    }
}
