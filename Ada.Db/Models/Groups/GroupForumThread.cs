using Ada.Core.Enums.Game.Groups;
using Ada.Db.Models.Players;

namespace Ada.Db.Models.Groups;

public class GroupForumThread
{
    public int Id { get; init; }
    public int GroupId { get; init; }
    public Group? Group { get; init; }
    public long PlayerId { get; init; }
    public Player? Player { get; init; }
    public required string Subject { get; set; }
    public bool IsPinned { get; set; }
    public bool IsLocked { get; set; }
    public ForumThreadState State { get; set; }
    public long AdminId { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<GroupForumMessage> Messages { get; init; } = [];
}
