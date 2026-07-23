using Microsoft.EntityFrameworkCore;
using Ada.API.DTOs;
using Ada.API.DTOs.Groups;
using Ada.API.Interfaces.Game.Groups;
using Ada.Core.Enums.Game.Groups;
using Ada.Db;

namespace Ada.Game.Groups;

public class GroupRepository(IDbContextFactory<AdaDbContext> dbContextFactory) : IGroupRepository
{
    public async Task<GroupDto?> GetByIdAsync(int groupId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        return await dbContext.Groups
            .AsNoTracking()
            .Where(x => x.Id == groupId)
            .Select(x => new GroupDto
            {
                Id = x.Id,
                PlayerId = x.PlayerId,
                Name = x.Name,
                Description = x.Description,
                RoomId = x.RoomId,
                CreatedAt = x.CreatedAt,
                Badge = x.Badge,
                ColorA = x.ColorA,
                ColorB = x.ColorB,
                Type = x.Type,
                AdminOnlyDecoration = x.AdminOnlyDecoration,
                HasForum = x.HasForum,
                ForumReadPermission = x.ForumReadPermission,
                ForumPostMessagesPermission = x.ForumPostMessagesPermission,
                ForumPostThreadsPermission = x.ForumPostThreadsPermission,
                ForumModPermission = x.ForumModPermission
            })
            .FirstOrDefaultAsync();
    }

    public async Task<int> GetMemberCountAsync(int groupId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        return await dbContext.GroupMemberships
            .CountAsync(x => x.GroupId == groupId && !x.IsPending);
    }

    public async Task<GroupMembershipDto?> GetMembershipAsync(int groupId, long playerId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        return await dbContext.GroupMemberships
            .AsNoTracking()
            .Where(x => x.GroupId == groupId && x.PlayerId == playerId)
            .Select(x => new GroupMembershipDto
            {
                GroupId = x.GroupId,
                PlayerId = x.PlayerId,
                Rank = x.Rank,
                IsPending = x.IsPending,
                CreatedAt = x.CreatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<bool> IsMemberAsync(int groupId, long playerId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        return await dbContext.GroupMemberships
            .AnyAsync(x => x.GroupId == groupId && x.PlayerId == playerId && !x.IsPending);
    }

    public async Task<(IReadOnlyList<GroupMemberDto> Members, int Total)> GetMembersAsync(
        int groupId, int page, string query, int levelId, int pageSize)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var members = dbContext.GroupMemberships
            .AsNoTracking()
            .Where(x => x.GroupId == groupId);

        members = levelId switch
        {
            1 => members.Where(x => !x.IsPending && x.Rank == GroupMemberRank.Admin),
            2 => members.Where(x => x.IsPending),
            _ => members.Where(x => !x.IsPending)
        };

        if (!string.IsNullOrWhiteSpace(query))
        {
            members = members.Where(x => x.Player!.Username.Contains(query));
        }

        var total = await members.CountAsync();

        var rows = await members
            .OrderByDescending(x => x.Rank)
            .ThenBy(x => x.CreatedAt)
            .Skip(page * pageSize)
            .Take(pageSize)
            .Select(x => new GroupMemberDto
            {
                PlayerId = x.PlayerId,
                Username = x.Player!.Username,
                FigureCode = x.Player.AvatarData!.FigureCode ?? "",
                Rank = x.Rank,
                IsPending = x.IsPending,
                JoinedAt = x.CreatedAt
            })
            .ToListAsync();

        return (rows, total);
    }

    public async Task<IReadOnlyList<GroupListItemDto>> GetGroupsForPlayerAsync(long playerId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        return await dbContext.GroupMemberships
            .AsNoTracking()
            .Where(x => x.PlayerId == playerId && !x.IsPending)
            .Select(x => new GroupListItemDto
            {
                Id = x.Group!.Id,
                Name = x.Group.Name,
                Badge = x.Group.Badge,
                ColorA = x.Group.ColorA,
                ColorB = x.Group.ColorB,
                IsFavourite = false,
                OwnerId = x.Group.PlayerId,
                HasForum = x.Group.HasForum
            })
            .ToListAsync();
    }

    public async Task<string?> GetRoomNameAsync(int roomId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        return await dbContext.Rooms
            .Where(x => x.Id == roomId)
            .Select(x => x.Name)
            .FirstOrDefaultAsync();
    }

    public bool IsOwner(GroupDto group, long playerId) => group.PlayerId == playerId;

    public async Task<bool> HasAdminRightsAsync(GroupDto group, long playerId)
    {
        if (IsOwner(group, playerId))
        {
            return true;
        }

        var membership = await GetMembershipAsync(group.Id, playerId);
        return membership is { IsPending: false, Rank: GroupMemberRank.Admin };
    }

    public async Task<IReadOnlyList<GroupCreationRoomDto>> GetRoomsForGroupCreationAsync(long playerId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        return await dbContext.Rooms
            .AsNoTracking()
            .Where(r => r.OwnerId == playerId && !dbContext.Groups.Any(g => g.RoomId == r.Id))
            .Select(r => new GroupCreationRoomDto { Id = r.Id, Name = r.Name })
            .ToListAsync();
    }

    public async Task<bool> RoomHasGroupAsync(int roomId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        return await dbContext.Groups.AnyAsync(g => g.RoomId == roomId);
    }

    public async Task<bool> PlayerOwnsRoomAsync(int roomId, long playerId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        return await dbContext.Rooms.AnyAsync(r => r.Id == roomId && r.OwnerId == playerId);
    }

    public async Task<int> CreateGroupAsync(
        long ownerId, int roomId, string name, string description, string badge,
        int colorA, int colorB, GroupType type)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var group = new Db.Models.Group
        {
            PlayerId = ownerId,
            RoomId = roomId,
            Name = name,
            Description = description,
            Badge = badge,
            ColorA = colorA,
            ColorB = colorB,
            Type = type,
            CreatedAt = (int) DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        dbContext.Groups.Add(group);
        await dbContext.SaveChangesAsync();

        dbContext.GroupMemberships.Add(new Db.Models.Groups.GroupMembership
        {
            GroupId = group.Id,
            PlayerId = ownerId,
            Rank = GroupMemberRank.Admin,
            IsPending = false,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync();

        return group.Id;
    }

    public async Task AddMembershipAsync(int groupId, long playerId, GroupMemberRank rank, bool isPending)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var exists = await dbContext.GroupMemberships
            .AnyAsync(x => x.GroupId == groupId && x.PlayerId == playerId);

        if (exists)
        {
            return;
        }

        dbContext.GroupMemberships.Add(new Db.Models.Groups.GroupMembership
        {
            GroupId = groupId,
            PlayerId = playerId,
            Rank = rank,
            IsPending = isPending,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync();
    }

    public async Task RemoveMembershipAsync(int groupId, long playerId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        await dbContext.GroupMemberships
            .Where(x => x.GroupId == groupId && x.PlayerId == playerId)
            .ExecuteDeleteAsync();
    }

    public async Task SetPendingAsync(int groupId, long playerId, bool isPending)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        await dbContext.GroupMemberships
            .Where(x => x.GroupId == groupId && x.PlayerId == playerId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsPending, isPending));
    }

    public async Task SetRankAsync(int groupId, long playerId, GroupMemberRank rank)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        await dbContext.GroupMemberships
            .Where(x => x.GroupId == groupId && x.PlayerId == playerId && !x.IsPending)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Rank, rank));
    }
}
