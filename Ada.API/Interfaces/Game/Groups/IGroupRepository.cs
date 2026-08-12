using Ada.API.DTOs;
using Ada.API.DTOs.Groups;
using Ada.Core.Enums.Game.Groups;

namespace Ada.API.Interfaces.Game.Groups;

public interface IGroupRepository
{
    Task<GroupDto?> GetByIdAsync(int groupId);
    Task<int> GetMemberCountAsync(int groupId);
    Task<GroupMembershipDto?> GetMembershipAsync(int groupId, long playerId);
    Task<bool> IsMemberAsync(int groupId, long playerId);
    Task<(IReadOnlyList<GroupMemberDto> Members, int Total)> GetMembersAsync(
        int groupId, int page, string query, int levelId, int pageSize);
    Task<IReadOnlyList<GroupListItemDto>> GetGroupsForPlayerAsync(long playerId);
    Task<string?> GetRoomNameAsync(int roomId);

    bool IsOwner(GroupDto group, long playerId);
    Task<bool> HasAdminRightsAsync(GroupDto group, long playerId);

    Task<IReadOnlyList<GroupCreationRoomDto>> GetRoomsForGroupCreationAsync(long playerId);
    Task<bool> RoomHasGroupAsync(int roomId);
    Task<bool> PlayerOwnsRoomAsync(int roomId, long playerId);
    Task<int> CreateGroupAsync(
        long ownerId, int roomId, string name, string description, string badge,
        int colorA, int colorB, GroupType type);

    Task UpdateInfoAsync(int groupId, string name, string description);
    Task UpdateColorsAsync(int groupId, int colorA, int colorB);
    Task UpdatePreferencesAsync(int groupId, GroupType type, bool adminOnlyDecoration);
    Task UpdateBadgeAsync(int groupId, string badge);
    Task DeleteGroupAsync(int groupId);

    Task AddMembershipAsync(int groupId, long playerId, GroupMemberRank rank, bool isPending);
    Task RemoveMembershipAsync(int groupId, long playerId);
    Task SetPendingAsync(int groupId, long playerId, bool isPending);
    Task SetRankAsync(int groupId, long playerId, GroupMemberRank rank);
}
