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
}
