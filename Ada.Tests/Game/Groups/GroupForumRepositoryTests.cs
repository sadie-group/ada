using Ada.Core.Enums.Game.Groups;
using Ada.Db.Models;
using Ada.Db.Models.Groups;
using Ada.Db.Models.Players;
using Ada.Db.Models.Rooms;
using Ada.Game.Groups;
using Ada.Tests.Common;

namespace Ada.Tests.Game.Groups;

[TestFixture]
public class GroupForumRepositoryTests
{
    private static readonly DateTimeOffset BaseTime = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static async Task SeedAsync(SqliteTestDbFactory factory)
    {
        await using var db = factory.CreateDbContext();

        db.Players.Add(new Player
        {
            Id = 1, Username = "alice", Email = "alice@test.com", Password = "secret",
            AvatarData = new PlayerAvatarData { FigureCode = "fig-a" }
        });
        db.Players.Add(new Player { Id = 2, Username = "bob", Email = "bob@test.com", Password = "secret" });

        db.RoomLayouts.Add(new RoomLayout { Id = 1 });
        db.Rooms.Add(new Room { Id = 101, Name = "Room101", Description = "", OwnerId = 1, LayoutId = 1 });
        db.Rooms.Add(new Room { Id = 102, Name = "Room102", Description = "", OwnerId = 1, LayoutId = 1 });
        db.Rooms.Add(new Room { Id = 103, Name = "Room103", Description = "", OwnerId = 1, LayoutId = 1 });

        db.Groups.Add(new Group { Id = 1, RoomId = 101, Name = "Forum One", Description = "first", Badge = "b1", HasForum = true });
        db.Groups.Add(new Group { Id = 2, RoomId = 102, Name = "Forum Two", Description = "second", Badge = "b2", HasForum = true });
        db.Groups.Add(new Group { Id = 3, RoomId = 103, Name = "No Forum", Description = "third", HasForum = false });

        db.GroupForumThreads.Add(new GroupForumThread
        {
            Id = 1, GroupId = 1, PlayerId = 1, Subject = "first thread",
            CreatedAt = BaseTime, UpdatedAt = BaseTime
        });
        db.GroupForumThreads.Add(new GroupForumThread
        {
            Id = 2, GroupId = 1, PlayerId = 2, Subject = "pinned thread", IsPinned = true,
            CreatedAt = BaseTime.AddMinutes(1), UpdatedAt = BaseTime.AddMinutes(1)
        });
        db.GroupForumThreads.Add(new GroupForumThread
        {
            Id = 3, GroupId = 1, PlayerId = 1, Subject = "hidden thread", State = ForumThreadState.Hidden,
            CreatedAt = BaseTime.AddMinutes(2), UpdatedAt = BaseTime.AddMinutes(2)
        });

        db.GroupForumMessages.Add(new GroupForumMessage
            { Id = 1, ThreadId = 1, PlayerId = 1, Message = "opener", CreatedAt = BaseTime });
        db.GroupForumMessages.Add(new GroupForumMessage
            { Id = 2, ThreadId = 1, PlayerId = 2, Message = "reply", CreatedAt = BaseTime.AddMinutes(1) });
        db.GroupForumMessages.Add(new GroupForumMessage
        {
            Id = 3, ThreadId = 1, PlayerId = 1, Message = "hidden reply", State = ForumMessageState.Hidden,
            CreatedAt = BaseTime.AddMinutes(2)
        });
        db.GroupForumMessages.Add(new GroupForumMessage
            { Id = 4, ThreadId = 2, PlayerId = 2, Message = "pinned opener", CreatedAt = BaseTime.AddMinutes(3) });

        await db.SaveChangesAsync();
    }

    private static async Task<(SqliteTestDbFactory Factory, GroupForumRepository Repository)> CreateSeededAsync()
    {
        var factory = new SqliteTestDbFactory();
        await SeedAsync(factory);
        return (factory, new GroupForumRepository(factory));
    }

    [Test]
    public async Task GetStatsAsync_UnknownGuild_ReturnsNull()
    {
        var (factory, repository) = await CreateSeededAsync();
        using var _ = factory;

        Assert.That(await repository.GetStatsAsync(99), Is.Null);
    }

    [Test]
    public async Task GetStatsAsync_GuildWithActivity_ComputesTotalsAndLastComment()
    {
        var (factory, repository) = await CreateSeededAsync();
        using var _ = factory;

        var stats = await repository.GetStatsAsync(1);

        Assert.That(stats, Is.Not.Null);
        Assert.That(stats!.GuildId, Is.EqualTo(1));
        Assert.That(stats.GuildName, Is.EqualTo("Forum One"));
        Assert.That(stats.GuildDescription, Is.EqualTo("first"));
        Assert.That(stats.Badge, Is.EqualTo("b1"));
        Assert.That(stats.TotalThreads, Is.EqualTo(2));
        Assert.That(stats.TotalComments, Is.EqualTo(3));
        Assert.That(stats.LastCommentThreadId, Is.EqualTo(2));
        Assert.That(stats.LastCommentUserId, Is.EqualTo(2));
        Assert.That(stats.LastCommentUsername, Is.EqualTo("bob"));
        Assert.That(stats.LastCommentAt, Is.EqualTo(BaseTime.AddMinutes(3)));
    }

    [Test]
    public async Task GetStatsAsync_GuildWithoutComments_ReturnsDefaults()
    {
        var (factory, repository) = await CreateSeededAsync();
        using var _ = factory;

        var stats = await repository.GetStatsAsync(2);

        Assert.That(stats, Is.Not.Null);
        Assert.That(stats!.TotalThreads, Is.EqualTo(0));
        Assert.That(stats.TotalComments, Is.EqualTo(0));
        Assert.That(stats.LastCommentThreadId, Is.EqualTo(-1));
        Assert.That(stats.LastCommentUserId, Is.EqualTo(-1));
        Assert.That(stats.LastCommentUsername, Is.EqualTo(""));
        Assert.That(stats.LastCommentAt, Is.Null);
    }

    [Test]
    public async Task GetForumsListAsync_OrdersByCommentCountAndExcludesForumlessGroups()
    {
        var (factory, repository) = await CreateSeededAsync();
        using var _ = factory;

        var (forums, total) = await repository.GetForumsListAsync(0, 0, 10);

        Assert.That(total, Is.EqualTo(2));
        Assert.That(forums.Select(f => f.GuildId), Is.EqualTo(new[] { 1, 2 }));
        Assert.That(forums[0].TotalThreads, Is.EqualTo(2));
        Assert.That(forums[0].TotalComments, Is.EqualTo(3));
        Assert.That(forums[0].LastCommentThreadId, Is.EqualTo(2));
        Assert.That(forums[0].LastCommentUsername, Is.EqualTo("bob"));
        Assert.That(forums[1].TotalThreads, Is.EqualTo(0));
        Assert.That(forums[1].TotalComments, Is.EqualTo(0));
        Assert.That(forums[1].LastCommentThreadId, Is.EqualTo(-1));
        Assert.That(forums[1].LastCommentUsername, Is.EqualTo(""));
    }

    [Test]
    public async Task GetForumsListAsync_Offset_Pages()
    {
        var (factory, repository) = await CreateSeededAsync();
        using var _ = factory;

        var (forums, total) = await repository.GetForumsListAsync(0, 1, 10);

        Assert.That(total, Is.EqualTo(2));
        Assert.That(forums, Has.Count.EqualTo(1));
        Assert.That(forums[0].GuildId, Is.EqualTo(2));
    }

    [Test]
    public async Task GetThreadsAsync_ReturnsOpenThreadsPinnedFirst()
    {
        var (factory, repository) = await CreateSeededAsync();
        using var _ = factory;

        var threads = await repository.GetThreadsAsync(1, 0, 10);

        Assert.That(threads.Select(t => t.ThreadId), Is.EqualTo(new[] { 2, 1 }));

        var first = threads[1];
        Assert.That(first.OpenerId, Is.EqualTo(1));
        Assert.That(first.OpenerUsername, Is.EqualTo("alice"));
        Assert.That(first.Subject, Is.EqualTo("first thread"));
        Assert.That(first.TotalComments, Is.EqualTo(2));
        Assert.That(first.LastAuthorId, Is.EqualTo(2));
        Assert.That(first.LastAuthorUsername, Is.EqualTo("bob"));
        Assert.That(first.LastCommentAt, Is.EqualTo(BaseTime.AddMinutes(1)));
    }

    [Test]
    public async Task GetThreadsAsync_StartIndex_Pages()
    {
        var (factory, repository) = await CreateSeededAsync();
        using var _ = factory;

        var threads = await repository.GetThreadsAsync(1, 1, 1);

        Assert.That(threads, Has.Count.EqualTo(1));
        Assert.That(threads[0].ThreadId, Is.EqualTo(1));
    }

    [Test]
    public async Task GetThreadAsync_HiddenThreadWithoutMessages_ReturnsDefaults()
    {
        var (factory, repository) = await CreateSeededAsync();
        using var _ = factory;

        var thread = await repository.GetThreadAsync(1, 3);

        Assert.That(thread, Is.Not.Null);
        Assert.That(thread!.State, Is.EqualTo((int) ForumThreadState.Hidden));
        Assert.That(thread.TotalComments, Is.EqualTo(0));
        Assert.That(thread.LastAuthorId, Is.EqualTo(-1));
        Assert.That(thread.LastAuthorUsername, Is.EqualTo(""));
        Assert.That(thread.LastCommentAt, Is.Null);
    }

    [Test]
    public async Task GetThreadAsync_WrongGuild_ReturnsNull()
    {
        var (factory, repository) = await CreateSeededAsync();
        using var _ = factory;

        Assert.That(await repository.GetThreadAsync(2, 1), Is.Null);
    }

    [Test]
    public async Task GetCommentAsync_ComputesIndexAndAuthorPostCount()
    {
        var (factory, repository) = await CreateSeededAsync();
        using var _ = factory;

        var comment = await repository.GetCommentAsync(1, 2);

        Assert.That(comment, Is.Not.Null);
        Assert.That(comment!.CommentId, Is.EqualTo(2));
        Assert.That(comment.Index, Is.EqualTo(1));
        Assert.That(comment.UserId, Is.EqualTo(2));
        Assert.That(comment.Username, Is.EqualTo("bob"));
        Assert.That(comment.FigureCode, Is.EqualTo(""));
        Assert.That(comment.Message, Is.EqualTo("reply"));
        Assert.That(comment.AuthorPostCount, Is.EqualTo(2));
    }

    [Test]
    public async Task GetCommentAsync_AuthorWithAvatar_ReturnsFigureCode()
    {
        var (factory, repository) = await CreateSeededAsync();
        using var _ = factory;

        var comment = await repository.GetCommentAsync(1, 1);

        Assert.That(comment, Is.Not.Null);
        Assert.That(comment!.FigureCode, Is.EqualTo("fig-a"));
        Assert.That(comment.Index, Is.EqualTo(0));
        Assert.That(comment.AuthorPostCount, Is.EqualTo(1));
    }

    [Test]
    public async Task GetCommentAsync_WrongGuild_ReturnsNull()
    {
        var (factory, repository) = await CreateSeededAsync();
        using var _ = factory;

        Assert.That(await repository.GetCommentAsync(2, 1), Is.Null);
    }

    [Test]
    public async Task GetCommentsAsync_ReturnsPageWithVisibleTotal()
    {
        var (factory, repository) = await CreateSeededAsync();
        using var _ = factory;

        var (comments, total) = await repository.GetCommentsAsync(1, 1, 0, 10);

        Assert.That(total, Is.EqualTo(2));
        Assert.That(comments, Has.Count.EqualTo(3));
        Assert.That(comments.Select(c => c.CommentId), Is.EqualTo(new[] { 1, 2, 3 }));
        Assert.That(comments.Select(c => c.Index), Is.EqualTo(new[] { 0, 1, 2 }));
        Assert.That(comments[2].State, Is.EqualTo((int) ForumMessageState.Hidden));
        Assert.That(comments[0].AuthorPostCount, Is.EqualTo(1));
        Assert.That(comments[1].AuthorPostCount, Is.EqualTo(2));
    }

    [Test]
    public async Task GetCommentsAsync_StartIndex_Pages()
    {
        var (factory, repository) = await CreateSeededAsync();
        using var _ = factory;

        var (comments, total) = await repository.GetCommentsAsync(1, 1, 1, 1);

        Assert.That(total, Is.EqualTo(2));
        Assert.That(comments, Has.Count.EqualTo(1));
        Assert.That(comments[0].CommentId, Is.EqualTo(2));
        Assert.That(comments[0].Index, Is.EqualTo(1));
    }

    [Test]
    public async Task PostThreadAsync_CreatesThreadWithOpeningComment()
    {
        var (factory, repository) = await CreateSeededAsync();
        using var _ = factory;

        var thread = await repository.PostThreadAsync(2, 1, "new subject", "new message");

        Assert.That(thread, Is.Not.Null);
        Assert.That(thread!.Subject, Is.EqualTo("new subject"));
        Assert.That(thread.OpenerId, Is.EqualTo(1));
        Assert.That(thread.OpenerUsername, Is.EqualTo("alice"));
        Assert.That(thread.TotalComments, Is.EqualTo(1));
        Assert.That(thread.LastAuthorId, Is.EqualTo(1));

        await using var db = factory.CreateDbContext();
        var message = await db.GroupForumMessages.SingleAsync(m => m.ThreadId == thread.ThreadId);
        Assert.That(message.Message, Is.EqualTo("new message"));
        Assert.That(message.PlayerId, Is.EqualTo(1));
    }

    [Test]
    public async Task PostCommentAsync_AppendsCommentAndBumpsThread()
    {
        var (factory, repository) = await CreateSeededAsync();
        using var _ = factory;

        var comment = await repository.PostCommentAsync(1, 1, 2, "another reply");

        Assert.That(comment, Is.Not.Null);
        Assert.That(comment!.Index, Is.EqualTo(2));
        Assert.That(comment.UserId, Is.EqualTo(2));
        Assert.That(comment.Username, Is.EqualTo("bob"));
        Assert.That(comment.FigureCode, Is.EqualTo(""));
        Assert.That(comment.Message, Is.EqualTo("another reply"));
        Assert.That(comment.State, Is.EqualTo((int) ForumMessageState.Visible));
        Assert.That(comment.AuthorPostCount, Is.EqualTo(3));

        await using var db = factory.CreateDbContext();
        var thread = await db.GroupForumThreads.SingleAsync(t => t.Id == 1);
        Assert.That(thread.UpdatedAt, Is.GreaterThan(BaseTime.AddMinutes(5)));
        Assert.That(await db.GroupForumMessages.CountAsync(m => m.ThreadId == 1), Is.EqualTo(4));
    }

    [Test]
    public async Task SetThreadPinnedLockedAsync_UpdatesFlags()
    {
        var (factory, repository) = await CreateSeededAsync();
        using var _ = factory;

        await repository.SetThreadPinnedLockedAsync(1, true, true);

        await using var db = factory.CreateDbContext();
        var thread = await db.GroupForumThreads.SingleAsync(t => t.Id == 1);
        Assert.That(thread.IsPinned, Is.True);
        Assert.That(thread.IsLocked, Is.True);
    }

    [Test]
    public async Task ModerateThreadAsync_UpdatesStateAndAdmin()
    {
        var (factory, repository) = await CreateSeededAsync();
        using var _ = factory;

        await repository.ModerateThreadAsync(1, ForumThreadState.HiddenByStaff, 42);

        await using var db = factory.CreateDbContext();
        var thread = await db.GroupForumThreads.SingleAsync(t => t.Id == 1);
        Assert.That(thread.State, Is.EqualTo(ForumThreadState.HiddenByStaff));
        Assert.That(thread.AdminId, Is.EqualTo(42));
    }

    [Test]
    public async Task ModerateCommentAsync_UpdatesStateAndAdmin()
    {
        var (factory, repository) = await CreateSeededAsync();
        using var _ = factory;

        await repository.ModerateCommentAsync(1, ForumMessageState.HiddenByStaff, 42);

        await using var db = factory.CreateDbContext();
        var message = await db.GroupForumMessages.SingleAsync(m => m.Id == 1);
        Assert.That(message.State, Is.EqualTo(ForumMessageState.HiddenByStaff));
        Assert.That(message.AdminId, Is.EqualTo(42));
    }

    [Test]
    public async Task UpdateForumSettingsAsync_UpdatesPermissions()
    {
        var (factory, repository) = await CreateSeededAsync();
        using var _ = factory;

        await repository.UpdateForumSettingsAsync(1,
            ForumPermissionLevel.Members, ForumPermissionLevel.Admins,
            ForumPermissionLevel.Owner, ForumPermissionLevel.Everyone);

        await using var db = factory.CreateDbContext();
        var group = await db.Groups.SingleAsync(g => g.Id == 1);
        Assert.That(group.ForumReadPermission, Is.EqualTo(ForumPermissionLevel.Members));
        Assert.That(group.ForumPostMessagesPermission, Is.EqualTo(ForumPermissionLevel.Admins));
        Assert.That(group.ForumPostThreadsPermission, Is.EqualTo(ForumPermissionLevel.Owner));
        Assert.That(group.ForumModPermission, Is.EqualTo(ForumPermissionLevel.Everyone));
    }

    [Test]
    public async Task GetThreadRefAsync_Found_ReturnsRef()
    {
        var (factory, repository) = await CreateSeededAsync();
        using var _ = factory;

        await repository.SetThreadPinnedLockedAsync(2, true, true);
        var reference = await repository.GetThreadRefAsync(2);

        Assert.That(reference, Is.Not.Null);
        Assert.That(reference!.Value.ThreadId, Is.EqualTo(2));
        Assert.That(reference.Value.GroupId, Is.EqualTo(1));
        Assert.That(reference.Value.IsLocked, Is.True);
    }

    [Test]
    public async Task GetThreadRefAsync_Unknown_ReturnsNull()
    {
        var (factory, repository) = await CreateSeededAsync();
        using var _ = factory;

        Assert.That(await repository.GetThreadRefAsync(99), Is.Null);
    }
}
