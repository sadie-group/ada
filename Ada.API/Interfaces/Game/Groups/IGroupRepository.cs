using Ada.API.DTOs;
using Ada.API.DTOs.Groups;

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
}
