using Ada.API.DTOs;
using Ada.API.DTOs.Groups;
using Ada.Core.Enums.Game.Groups;

namespace Ada.Networking.Events.Handlers.Groups;

public record ForumPermissionResult
{
    public bool CanRead { get; init; }
    public bool CanPost { get; init; }
    public bool CanThread { get; init; }
    public bool CanModerate { get; init; }
    public bool CanChangeSettings { get; init; }
    public string ErrorRead { get; init; } = "";
    public string ErrorPost { get; init; } = "";
    public string ErrorStartThread { get; init; } = "";
    public string ErrorModerate { get; init; } = "";
}

public static class ForumPermissions
{
    public static int LevelFor(GroupDto group, GroupMembershipDto? membership, long playerId)
    {
        if (group.PlayerId == playerId)
        {
            return 3;
        }

        if (membership is not { IsPending: false })
        {
            return 0;
        }

        return membership.Rank == GroupMemberRank.Admin ? 2 : 1;
    }

    public static ForumPermissionResult Evaluate(GroupDto group, int level)
    {
        var canRead = level >= (int) group.ForumReadPermission;
        var canPost = level >= (int) group.ForumPostMessagesPermission;
        var canThread = level >= (int) group.ForumPostThreadsPermission;
        var canMod = level >= (int) group.ForumModPermission;

        return new ForumPermissionResult
        {
            CanRead = canRead,
            CanPost = canPost,
            CanThread = canThread,
            CanModerate = canMod,
            CanChangeSettings = level >= 3,
            ErrorRead = Error(canRead, group.ForumReadPermission),
            ErrorPost = Error(canPost, group.ForumPostMessagesPermission),
            ErrorStartThread = Error(canThread, group.ForumPostThreadsPermission),
            ErrorModerate = Error(canMod, group.ForumModPermission)
        };
    }

    private static string Error(bool allowed, ForumPermissionLevel required)
    {
        if (allowed)
        {
            return "";
        }

        return required switch
        {
            ForumPermissionLevel.Owner => "not_owner",
            ForumPermissionLevel.Admins => "not_admin",
            _ => "not_member"
        };
    }
}
