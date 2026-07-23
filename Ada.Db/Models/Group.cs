using Ada.Core.Enums.Game.Groups;
using Ada.Db.Models.Groups;
using Ada.Db.Models.Players;

namespace Ada.Db.Models;

public class Group
{
    public int Id { get; init; }
    public long PlayerId { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public int RoomId { get; set; }
    public int CreatedAt { get; init; }

    public string Badge { get; set; } = "";
    public int ColorA { get; set; }
    public int ColorB { get; set; }
    public GroupType Type { get; set; }
    public bool AdminOnlyDecoration { get; set; }
    public bool HasForum { get; set; }

    public ForumPermissionLevel ForumReadPermission { get; set; } = ForumPermissionLevel.Everyone;
    public ForumPermissionLevel ForumPostMessagesPermission { get; set; } = ForumPermissionLevel.Members;
    public ForumPermissionLevel ForumPostThreadsPermission { get; set; } = ForumPermissionLevel.Members;
    public ForumPermissionLevel ForumModPermission { get; set; } = ForumPermissionLevel.Admins;

    public ICollection<Player> Players { get; init; } = [];
    public ICollection<GroupMembership> Memberships { get; init; } = [];
    public ICollection<GroupForumThread> ForumThreads { get; init; } = [];
}
