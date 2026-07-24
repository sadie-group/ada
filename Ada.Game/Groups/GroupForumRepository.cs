using Microsoft.EntityFrameworkCore;
using Ada.API.DTOs.Groups;
using Ada.API.Interfaces.Game.Groups;
using Ada.Core.Enums.Game.Groups;
using Ada.Db;
using Ada.Db.Models.Groups;

namespace Ada.Game.Groups;

public class GroupForumRepository(IDbContextFactory<AdaDbContext> dbContextFactory) : IGroupForumRepository
{
    public async Task<ForumStatsDto?> GetStatsAsync(int guildId)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();

        var group = await db.Groups
            .AsNoTracking()
            .Where(g => g.Id == guildId)
            .Select(g => new { g.Id, g.Name, g.Description, g.Badge })
            .FirstOrDefaultAsync();

        return group == null ? null : await BuildStatsAsync(db, group.Id, group.Name, group.Description, group.Badge);
    }

    public async Task<(IReadOnlyList<ForumStatsDto> Forums, int Total)> GetForumsListAsync(int mode, int offset, int amount)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();

        var forums = db.Groups.AsNoTracking().Where(g => g.HasForum);

        var total = await forums.CountAsync();

        var rows = await forums
            .OrderByDescending(g => db.GroupForumMessages.Count(m => m.Thread!.GroupId == g.Id))
            .Skip(offset)
            .Take(amount)
            .Select(g => new { g.Id, g.Name, g.Description, g.Badge })
            .ToListAsync();

        var guildIds = rows.Select(r => r.Id).ToArray();

        var threadCounts = await db.GroupForumThreads
            .Where(t => guildIds.Contains(t.GroupId) && t.State == ForumThreadState.Open)
            .GroupBy(t => t.GroupId)
            .Select(x => new { GuildId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.GuildId, x => x.Count);

        var commentCounts = await db.GroupForumMessages
            .Where(m => guildIds.Contains(m.Thread!.GroupId) && m.State == ForumMessageState.Visible)
            .GroupBy(m => m.Thread!.GroupId)
            .Select(x => new { GuildId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.GuildId, x => x.Count);

        var lastIds = await db.GroupForumMessages
            .Where(m => guildIds.Contains(m.Thread!.GroupId) && m.State == ForumMessageState.Visible)
            .GroupBy(m => m.Thread!.GroupId)
            .Select(x => x.Max(m => m.Id))
            .ToListAsync();

        var lastComments = (await db.GroupForumMessages
                .AsNoTracking()
                .Where(m => lastIds.Contains(m.Id))
                .Select(m => new { GuildId = m.Thread!.GroupId, m.ThreadId, m.PlayerId, Username = m.Player!.Username, m.CreatedAt })
                .ToListAsync())
            .ToDictionary(x => x.GuildId);

        var stats = new List<ForumStatsDto>(rows.Count);

        foreach (var g in rows)
        {
            lastComments.TryGetValue(g.Id, out var last);

            stats.Add(new ForumStatsDto
            {
                GuildId = g.Id,
                GuildName = g.Name,
                GuildDescription = g.Description,
                Badge = g.Badge,
                TotalThreads = threadCounts.GetValueOrDefault(g.Id),
                TotalComments = commentCounts.GetValueOrDefault(g.Id),
                UnreadComments = 0,
                LastCommentThreadId = last?.ThreadId ?? -1,
                LastCommentUserId = last?.PlayerId ?? -1,
                LastCommentUsername = last?.Username ?? "",
                LastCommentAt = last?.CreatedAt
            });
        }

        return (stats, total);
    }

    public async Task<IReadOnlyList<ForumThreadDto>> GetThreadsAsync(int guildId, int startIndex, int amount)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();

        var rows = await ThreadQuery(db, db.GroupForumThreads
                .AsNoTracking()
                .Where(t => t.GroupId == guildId && t.State == ForumThreadState.Open))
            .OrderByDescending(t => t.IsPinned)
            .ThenByDescending(t => t.LastCommentAt ?? t.CreatedAt)
            .Skip(startIndex)
            .Take(amount)
            .ToListAsync();

        return rows;
    }

    public async Task<ForumThreadDto?> GetThreadAsync(int guildId, int threadId)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();

        return await ThreadQuery(db, db.GroupForumThreads
                .AsNoTracking()
                .Where(t => t.GroupId == guildId && t.Id == threadId))
            .FirstOrDefaultAsync();
    }

    public async Task<ForumCommentDto?> GetCommentAsync(int guildId, int commentId)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();

        var m = await db.GroupForumMessages
            .AsNoTracking()
            .Where(x => x.Id == commentId && x.Thread!.GroupId == guildId)
            .Select(x => new
            {
                x.Id, x.ThreadId, x.PlayerId,
                Username = x.Player!.Username,
                Figure = x.Player.AvatarData!.FigureCode,
                x.CreatedAt, x.Message, x.State, x.AdminId
            })
            .FirstOrDefaultAsync();

        if (m == null)
        {
            return null;
        }

        var index = await db.GroupForumMessages
            .CountAsync(x => x.ThreadId == m.ThreadId && x.Id < m.Id && x.State == ForumMessageState.Visible);

        var authorPostCount = await db.GroupForumMessages
            .CountAsync(x => x.Thread!.GroupId == guildId && x.PlayerId == m.PlayerId &&
                             x.State == ForumMessageState.Visible);

        return new ForumCommentDto
        {
            CommentId = m.Id,
            Index = index,
            UserId = m.PlayerId,
            Username = m.Username,
            FigureCode = m.Figure ?? "",
            CreatedAt = m.CreatedAt,
            Message = m.Message,
            State = (int) m.State,
            AdminId = m.AdminId,
            AdminUsername = "",
            AuthorPostCount = authorPostCount
        };
    }

    public async Task<(IReadOnlyList<ForumCommentDto> Comments, int Total)> GetCommentsAsync(
        int guildId, int threadId, int startIndex, int amount)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();

        var messages = db.GroupForumMessages
            .AsNoTracking()
            .Where(m => m.ThreadId == threadId && m.Thread!.GroupId == guildId);

        var total = await messages.CountAsync(m => m.State == ForumMessageState.Visible);

        var page = await messages
            .OrderBy(m => m.Id)
            .Skip(startIndex)
            .Take(amount)
            .Select(m => new
            {
                m.Id,
                m.PlayerId,
                Username = m.Player!.Username,
                Figure = m.Player.AvatarData!.FigureCode,
                m.CreatedAt,
                m.Message,
                m.State,
                m.AdminId
            })
            .ToListAsync();

        var authorIds = page.Select(m => m.PlayerId).Distinct().ToList();
        var counts = await db.GroupForumMessages
            .AsNoTracking()
            .Where(m => m.Thread!.GroupId == guildId && authorIds.Contains(m.PlayerId) &&
                        m.State == ForumMessageState.Visible)
            .GroupBy(m => m.PlayerId)
            .Select(grp => new { PlayerId = grp.Key, Count = grp.Count() })
            .ToDictionaryAsync(x => x.PlayerId, x => x.Count);

        var comments = page.Select((m, i) => new ForumCommentDto
        {
            CommentId = m.Id,
            Index = startIndex + i,
            UserId = m.PlayerId,
            Username = m.Username,
            FigureCode = m.Figure ?? "",
            CreatedAt = m.CreatedAt,
            Message = m.Message,
            State = (int) m.State,
            AdminId = m.AdminId,
            AdminUsername = "",
            AuthorPostCount = counts.GetValueOrDefault(m.PlayerId)
        }).ToList();

        return (comments, total);
    }

    public async Task<ForumThreadDto?> PostThreadAsync(int guildId, long playerId, string subject, string message)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();

        var now = DateTimeOffset.UtcNow;

        var thread = new GroupForumThread
        {
            GroupId = guildId,
            PlayerId = playerId,
            Subject = subject,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.GroupForumThreads.Add(thread);
        await db.SaveChangesAsync();

        db.GroupForumMessages.Add(new GroupForumMessage
        {
            ThreadId = thread.Id,
            PlayerId = playerId,
            Message = message,
            CreatedAt = now
        });

        await db.SaveChangesAsync();

        return await GetThreadAsync(guildId, thread.Id);
    }

    public async Task<ForumCommentDto?> PostCommentAsync(int guildId, int threadId, long playerId, string message)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();

        var now = DateTimeOffset.UtcNow;
        var indexBefore = await db.GroupForumMessages
            .CountAsync(m => m.ThreadId == threadId && m.State == ForumMessageState.Visible);

        var comment = new GroupForumMessage
        {
            ThreadId = threadId,
            PlayerId = playerId,
            Message = message,
            CreatedAt = now
        };

        db.GroupForumMessages.Add(comment);

        await db.GroupForumThreads
            .Where(t => t.Id == threadId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UpdatedAt, now));

        await db.SaveChangesAsync();

        var authorPostCount = await db.GroupForumMessages
            .CountAsync(m => m.Thread!.GroupId == guildId && m.PlayerId == playerId &&
                             m.State == ForumMessageState.Visible);

        var author = await db.Players
            .AsNoTracking()
            .Where(p => p.Id == playerId)
            .Select(p => new { p.Username, Figure = p.AvatarData!.FigureCode })
            .FirstOrDefaultAsync();

        return new ForumCommentDto
        {
            CommentId = comment.Id,
            Index = indexBefore,
            UserId = playerId,
            Username = author?.Username ?? "",
            FigureCode = author?.Figure ?? "",
            CreatedAt = now,
            Message = message,
            State = (int) ForumMessageState.Visible,
            AdminId = 0,
            AdminUsername = "",
            AuthorPostCount = authorPostCount
        };
    }

    public async Task SetThreadPinnedLockedAsync(int threadId, bool isPinned, bool isLocked)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();

        await db.GroupForumThreads
            .Where(t => t.Id == threadId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.IsPinned, isPinned)
                .SetProperty(t => t.IsLocked, isLocked));
    }

    public async Task ModerateThreadAsync(int threadId, ForumThreadState state, long adminId)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();

        await db.GroupForumThreads
            .Where(t => t.Id == threadId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.State, state)
                .SetProperty(t => t.AdminId, adminId));
    }

    public async Task ModerateCommentAsync(int commentId, ForumMessageState state, long adminId)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();

        await db.GroupForumMessages
            .Where(m => m.Id == commentId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.State, state)
                .SetProperty(m => m.AdminId, adminId));
    }

    public async Task UpdateForumSettingsAsync(
        int guildId, ForumPermissionLevel canRead, ForumPermissionLevel postMessages,
        ForumPermissionLevel postThreads, ForumPermissionLevel modForum)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();

        await db.Groups
            .Where(g => g.Id == guildId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(g => g.ForumReadPermission, canRead)
                .SetProperty(g => g.ForumPostMessagesPermission, postMessages)
                .SetProperty(g => g.ForumPostThreadsPermission, postThreads)
                .SetProperty(g => g.ForumModPermission, modForum));
    }

    public async Task<(int ThreadId, int GroupId, bool IsLocked)?> GetThreadRefAsync(int threadId)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();

        var row = await db.GroupForumThreads
            .AsNoTracking()
            .Where(t => t.Id == threadId)
            .Select(t => new { t.Id, t.GroupId, t.IsLocked })
            .FirstOrDefaultAsync();

        return row == null ? null : (row.Id, row.GroupId, row.IsLocked);
    }

    private static IQueryable<ForumThreadDto> ThreadQuery(AdaDbContext db, IQueryable<GroupForumThread> source)
    {
        return source.Select(t => new ForumThreadDto
        {
            ThreadId = t.Id,
            OpenerId = t.PlayerId,
            OpenerUsername = t.Player!.Username,
            Subject = t.Subject,
            IsPinned = t.IsPinned,
            IsLocked = t.IsLocked,
            CreatedAt = t.CreatedAt,
            TotalComments = db.GroupForumMessages.Count(m => m.ThreadId == t.Id && m.State == ForumMessageState.Visible),
            UnreadComments = 0,
            LastAuthorId = db.GroupForumMessages
                .Where(m => m.ThreadId == t.Id && m.State == ForumMessageState.Visible)
                .OrderByDescending(m => m.Id)
                .Select(m => (long?) m.PlayerId)
                .FirstOrDefault() ?? -1,
            LastAuthorUsername = db.GroupForumMessages
                .Where(m => m.ThreadId == t.Id && m.State == ForumMessageState.Visible)
                .OrderByDescending(m => m.Id)
                .Select(m => m.Player!.Username)
                .FirstOrDefault() ?? "",
            LastCommentAt = db.GroupForumMessages
                .Where(m => m.ThreadId == t.Id && m.State == ForumMessageState.Visible)
                .OrderByDescending(m => m.Id)
                .Select(m => (DateTimeOffset?) m.CreatedAt)
                .FirstOrDefault(),
            State = (int) t.State,
            AdminId = t.AdminId,
            AdminUsername = ""
        });
    }

    private static async Task<ForumStatsDto> BuildStatsAsync(
        AdaDbContext db, int guildId, string name, string description, string badge)
    {
        var totalThreads = await db.GroupForumThreads
            .CountAsync(t => t.GroupId == guildId && t.State == ForumThreadState.Open);

        var totalComments = await db.GroupForumMessages
            .CountAsync(m => m.Thread!.GroupId == guildId && m.State == ForumMessageState.Visible);

        var last = await db.GroupForumMessages
            .AsNoTracking()
            .Where(m => m.Thread!.GroupId == guildId && m.State == ForumMessageState.Visible)
            .OrderByDescending(m => m.Id)
            .Select(m => new { m.ThreadId, m.PlayerId, Username = m.Player!.Username, m.CreatedAt })
            .FirstOrDefaultAsync();

        return new ForumStatsDto
        {
            GuildId = guildId,
            GuildName = name,
            GuildDescription = description,
            Badge = badge,
            TotalThreads = totalThreads,
            TotalComments = totalComments,
            UnreadComments = 0,
            LastCommentThreadId = last?.ThreadId ?? -1,
            LastCommentUserId = last?.PlayerId ?? -1,
            LastCommentUsername = last?.Username ?? "",
            LastCommentAt = last?.CreatedAt
        };
    }
}
