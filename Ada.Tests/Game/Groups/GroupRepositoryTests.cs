using Ada.Core.Enums.Game.Groups;
using Ada.Db.Models;
using Ada.Db.Models.Groups;
using Ada.Db.Models.Players;
using Ada.Db.Models.Rooms;
using Ada.Game.Groups;
using Ada.Tests.Common;

namespace Ada.Tests.Game.Groups;

[TestFixture]
public class GroupRepositoryTests
{
    private SqliteTestDbFactory _factory = null!;
    private GroupRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new SqliteTestDbFactory();
        _repository = new GroupRepository(_factory);
    }

    [TearDown]
    public void TearDown() => _factory.Dispose();

    private async Task SeedPlayerAsync(long id, string username, string? figureCode = "fig")
    {
        await using var db = _factory.CreateDbContext();
        db.Players.Add(new Player
        {
            Id = id,
            Username = username,
            Email = "",
            Password = "",
            AvatarData = figureCode == null ? null : new PlayerAvatarData { FigureCode = figureCode }
        });
        await db.SaveChangesAsync();
    }

    private async Task SeedGroupAsync(int id, long ownerId = 1, int roomId = 0, GroupType type = GroupType.Open)
    {
        if (roomId == 0)
        {
            roomId = 100 + id;
        }

        await EnsureRoomAsync(roomId);

        await using var db = _factory.CreateDbContext();
        db.Groups.Add(new Group
        {
            Id = id,
            PlayerId = ownerId,
            RoomId = roomId,
            Name = $"Group{id}",
            Description = $"Desc{id}",
            Badge = "b1",
            ColorA = 2,
            ColorB = 3,
            Type = type,
            CreatedAt = 1000
        });
        await db.SaveChangesAsync();
    }

    private async Task EnsureRoomAsync(int roomId)
    {
        await using var db = _factory.CreateDbContext();
        if (await db.Rooms.AnyAsync(r => r.Id == roomId))
        {
            return;
        }

        var ownerId = 1000 + roomId;
        if (!await db.Players.AnyAsync(p => p.Id == ownerId))
        {
            db.Players.Add(new Player { Id = ownerId, Username = $"sys{roomId}", Email = "", Password = "" });
        }

        if (!await db.RoomLayouts.AnyAsync(l => l.Id == 1))
        {
            db.RoomLayouts.Add(new RoomLayout { Id = 1 });
        }

        db.Rooms.Add(new Room { Id = roomId, Name = $"Room{roomId}", Description = "", OwnerId = ownerId, LayoutId = 1 });
        await db.SaveChangesAsync();
    }

    private async Task SeedMembershipAsync(int groupId, long playerId, GroupMemberRank rank = GroupMemberRank.Member,
        bool isPending = false, long createdAtSeconds = 0)
    {
        await using var db = _factory.CreateDbContext();
        db.GroupMemberships.Add(new GroupMembership
        {
            GroupId = groupId,
            PlayerId = playerId,
            Rank = rank,
            IsPending = isPending,
            CreatedAt = DateTimeOffset.FromUnixTimeSeconds(createdAtSeconds)
        });
        await db.SaveChangesAsync();
    }

    private async Task SeedRoomAsync(int id, long ownerId, string name)
    {
        await using var db = _factory.CreateDbContext();
        if (!await db.RoomLayouts.AnyAsync(l => l.Id == 1))
        {
            db.RoomLayouts.Add(new RoomLayout { Id = 1 });
        }

        db.Rooms.Add(new Room { Id = id, Name = name, Description = "", OwnerId = ownerId, LayoutId = 1 });
        await db.SaveChangesAsync();
    }

    [Test]
    public async Task GetByIdAsync_Exists_MapsAllFields()
    {
        await SeedPlayerAsync(1, "owner");
        await SeedGroupAsync(5, ownerId: 1, roomId: 9, type: GroupType.Request);

        var group = await _repository.GetByIdAsync(5);

        Assert.That(group, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(group.Id, Is.EqualTo(5));
            Assert.That(group.PlayerId, Is.EqualTo(1));
            Assert.That(group.Name, Is.EqualTo("Group5"));
            Assert.That(group.Description, Is.EqualTo("Desc5"));
            Assert.That(group.RoomId, Is.EqualTo(9));
            Assert.That(group.CreatedAt, Is.EqualTo(1000));
            Assert.That(group.Badge, Is.EqualTo("b1"));
            Assert.That(group.ColorA, Is.EqualTo(2));
            Assert.That(group.ColorB, Is.EqualTo(3));
            Assert.That(group.Type, Is.EqualTo(GroupType.Request));
        });
    }

    [Test]
    public async Task GetByIdAsync_Missing_ReturnsNull()
    {
        Assert.That(await _repository.GetByIdAsync(404), Is.Null);
    }

    [Test]
    public async Task GetMemberCountAsync_ExcludesPending()
    {
        await SeedPlayerAsync(1, "a");
        await SeedPlayerAsync(2, "b");
        await SeedPlayerAsync(3, "c");
        await SeedGroupAsync(1);
        await SeedMembershipAsync(1, 1);
        await SeedMembershipAsync(1, 2);
        await SeedMembershipAsync(1, 3, isPending: true);

        Assert.That(await _repository.GetMemberCountAsync(1), Is.EqualTo(2));
    }

    [Test]
    public async Task GetMembershipAsync_Exists_MapsFields()
    {
        await SeedPlayerAsync(1, "a");
        await SeedGroupAsync(1);
        await SeedMembershipAsync(1, 1, GroupMemberRank.Admin, isPending: true, createdAtSeconds: 50);

        var membership = await _repository.GetMembershipAsync(1, 1);

        Assert.That(membership, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(membership.GroupId, Is.EqualTo(1));
            Assert.That(membership.PlayerId, Is.EqualTo(1));
            Assert.That(membership.Rank, Is.EqualTo(GroupMemberRank.Admin));
            Assert.That(membership.IsPending, Is.True);
            Assert.That(membership.CreatedAt, Is.EqualTo(DateTimeOffset.FromUnixTimeSeconds(50)));
        });
    }

    [Test]
    public async Task GetMembershipAsync_Missing_ReturnsNull()
    {
        Assert.That(await _repository.GetMembershipAsync(1, 1), Is.Null);
    }

    [Test]
    public async Task IsMemberAsync_NonPendingMember_True()
    {
        await SeedPlayerAsync(1, "a");
        await SeedGroupAsync(1);
        await SeedMembershipAsync(1, 1);

        Assert.That(await _repository.IsMemberAsync(1, 1), Is.True);
    }

    [Test]
    public async Task IsMemberAsync_PendingMember_False()
    {
        await SeedPlayerAsync(1, "a");
        await SeedGroupAsync(1);
        await SeedMembershipAsync(1, 1, isPending: true);

        Assert.That(await _repository.IsMemberAsync(1, 1), Is.False);
    }

    [Test]
    public async Task IsMemberAsync_NoMembership_False()
    {
        Assert.That(await _repository.IsMemberAsync(1, 1), Is.False);
    }

    private async Task SeedMembersFixtureAsync()
    {
        await SeedPlayerAsync(10, "alice");
        await SeedPlayerAsync(11, "bob");
        await SeedPlayerAsync(12, "carol");
        await SeedPlayerAsync(13, "dave", figureCode: null);
        await SeedGroupAsync(1);
        await SeedMembershipAsync(1, 10, GroupMemberRank.Admin, createdAtSeconds: 10);
        await SeedMembershipAsync(1, 11, createdAtSeconds: 20);
        await SeedMembershipAsync(1, 12, isPending: true, createdAtSeconds: 30);
        await SeedMembershipAsync(1, 13, createdAtSeconds: 40);
    }

    [Test]
    public async Task GetMembersAsync_DefaultLevel_ReturnsNonPendingOrderedByRank()
    {
        await SeedMembersFixtureAsync();

        var (members, total) = await _repository.GetMembersAsync(1, 0, "", 0, 10);
        Assert.Multiple(() =>
        {
            Assert.That(total, Is.EqualTo(3));
            Assert.That(members.Select(m => m.Username), Is.EqualTo(new[] { "alice", "bob", "dave" }));
            Assert.That(members[0].Rank, Is.EqualTo(GroupMemberRank.Admin));
            Assert.That(members[0].FigureCode, Is.EqualTo("fig"));
            Assert.That(members[2].FigureCode, Is.EqualTo(""));
        });
    }

    [Test]
    public async Task GetMembersAsync_AdminLevel_ReturnsAdminsOnly()
    {
        await SeedMembersFixtureAsync();

        var (members, total) = await _repository.GetMembersAsync(1, 0, "", 1, 10);
        Assert.Multiple(() =>
        {
            Assert.That(total, Is.EqualTo(1));
            Assert.That(members.Single().Username, Is.EqualTo("alice"));
        });
    }

    [Test]
    public async Task GetMembersAsync_PendingLevel_ReturnsPendingOnly()
    {
        await SeedMembersFixtureAsync();

        var (members, total) = await _repository.GetMembersAsync(1, 0, "", 2, 10);
        Assert.Multiple(() =>
        {
            Assert.That(total, Is.EqualTo(1));
            Assert.That(members.Single().Username, Is.EqualTo("carol"));
            Assert.That(members.Single().IsPending, Is.True);
        });
    }

    [Test]
    public async Task GetMembersAsync_Query_FiltersByUsername()
    {
        await SeedMembersFixtureAsync();

        var (members, total) = await _repository.GetMembersAsync(1, 0, "bo", 0, 10);
        Assert.Multiple(() =>
        {
            Assert.That(total, Is.EqualTo(1));
            Assert.That(members.Single().Username, Is.EqualTo("bob"));
        });
    }

    [Test]
    public async Task GetMembersAsync_Paging_SkipsAndTakes()
    {
        await SeedMembersFixtureAsync();

        var (members, total) = await _repository.GetMembersAsync(1, 1, "", 0, 2);
        Assert.Multiple(() =>
        {
            Assert.That(total, Is.EqualTo(3));
            Assert.That(members.Single().Username, Is.EqualTo("dave"));
        });
    }

    [Test]
    public async Task GetGroupsForPlayerAsync_ExcludesPendingMemberships()
    {
        await SeedPlayerAsync(1, "a");
        await SeedGroupAsync(1, ownerId: 7);
        await SeedGroupAsync(2, ownerId: 8);
        await SeedMembershipAsync(1, 1);
        await SeedMembershipAsync(2, 1, isPending: true);

        var groups = await _repository.GetGroupsForPlayerAsync(1);

        Assert.That(groups, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(groups[0].Id, Is.EqualTo(1));
            Assert.That(groups[0].Name, Is.EqualTo("Group1"));
            Assert.That(groups[0].Badge, Is.EqualTo("b1"));
            Assert.That(groups[0].OwnerId, Is.EqualTo(7));
        });
    }

    [Test]
    public async Task GetRoomNameAsync_Exists_ReturnsName()
    {
        await SeedPlayerAsync(1, "a");
        await SeedRoomAsync(3, 1, "Lobby");

        Assert.That(await _repository.GetRoomNameAsync(3), Is.EqualTo("Lobby"));
    }

    [Test]
    public async Task GetRoomNameAsync_Missing_ReturnsNull()
    {
        Assert.That(await _repository.GetRoomNameAsync(3), Is.Null);
    }

    [Test]
    public async Task IsOwner_ComparesPlayerId()
    {
        await SeedPlayerAsync(1, "a");
        await SeedGroupAsync(1, ownerId: 1);
        var group = (await _repository.GetByIdAsync(1))!;
        Assert.Multiple(() =>
        {
            Assert.That(_repository.IsOwner(group, 1), Is.True);
            Assert.That(_repository.IsOwner(group, 2), Is.False);
        });
    }

    [Test]
    public async Task HasAdminRightsAsync_Owner_True()
    {
        await SeedPlayerAsync(1, "a");
        await SeedGroupAsync(1, ownerId: 1);
        var group = (await _repository.GetByIdAsync(1))!;

        Assert.That(await _repository.HasAdminRightsAsync(group, 1), Is.True);
    }

    [Test]
    public async Task HasAdminRightsAsync_AdminMember_True()
    {
        await SeedPlayerAsync(1, "a");
        await SeedPlayerAsync(2, "b");
        await SeedGroupAsync(1, ownerId: 1);
        await SeedMembershipAsync(1, 2, GroupMemberRank.Admin);
        var group = (await _repository.GetByIdAsync(1))!;

        Assert.That(await _repository.HasAdminRightsAsync(group, 2), Is.True);
    }

    [Test]
    public async Task HasAdminRightsAsync_RegularMember_False()
    {
        await SeedPlayerAsync(1, "a");
        await SeedPlayerAsync(2, "b");
        await SeedGroupAsync(1, ownerId: 1);
        await SeedMembershipAsync(1, 2);
        var group = (await _repository.GetByIdAsync(1))!;

        Assert.That(await _repository.HasAdminRightsAsync(group, 2), Is.False);
    }

    [Test]
    public async Task HasAdminRightsAsync_NoMembership_False()
    {
        await SeedPlayerAsync(1, "a");
        await SeedGroupAsync(1, ownerId: 1);
        var group = (await _repository.GetByIdAsync(1))!;

        Assert.That(await _repository.HasAdminRightsAsync(group, 99), Is.False);
    }

    [Test]
    public async Task GetRoomsForGroupCreationAsync_ExcludesRoomsWithGroupsAndOtherOwners()
    {
        await SeedPlayerAsync(1, "a");
        await SeedPlayerAsync(2, "b");
        await SeedRoomAsync(1, 1, "Free");
        await SeedRoomAsync(2, 1, "Taken");
        await SeedRoomAsync(3, 2, "Other");
        await SeedGroupAsync(1, ownerId: 1, roomId: 2);

        var rooms = await _repository.GetRoomsForGroupCreationAsync(1);

        Assert.That(rooms, Has.Count.EqualTo(1));
        Assert.That(rooms[0].Name, Is.EqualTo("Free"));
    }

    [Test]
    public async Task RoomHasGroupAsync_ChecksRoomId()
    {
        await SeedGroupAsync(1, roomId: 5);
        Assert.Multiple(async () =>
        {
            Assert.That(await _repository.RoomHasGroupAsync(5), Is.True);
            Assert.That(await _repository.RoomHasGroupAsync(6), Is.False);
        });
    }

    [Test]
    public async Task PlayerOwnsRoomAsync_ChecksOwner()
    {
        await SeedPlayerAsync(1, "a");
        await SeedRoomAsync(1, 1, "Room");
        Assert.Multiple(async () =>
        {
            Assert.That(await _repository.PlayerOwnsRoomAsync(1, 1), Is.True);
            Assert.That(await _repository.PlayerOwnsRoomAsync(1, 2), Is.False);
            Assert.That(await _repository.PlayerOwnsRoomAsync(2, 1), Is.False);
        });
    }

    [Test]
    public async Task CreateGroupAsync_CreatesGroupAndAdminMembership()
    {
        await SeedPlayerAsync(1, "a");
        await SeedRoomAsync(4, 1, "Room4");

        var groupId = await _repository.CreateGroupAsync(1, 4, "Name", "Desc", "badge", 5, 6, GroupType.Closed);

        Assert.That(groupId, Is.GreaterThan(0));

        var group = (await _repository.GetByIdAsync(groupId))!;
        Assert.Multiple(() =>
        {
            Assert.That(group.PlayerId, Is.EqualTo(1));
            Assert.That(group.RoomId, Is.EqualTo(4));
            Assert.That(group.Name, Is.EqualTo("Name"));
            Assert.That(group.Description, Is.EqualTo("Desc"));
            Assert.That(group.Badge, Is.EqualTo("badge"));
            Assert.That(group.ColorA, Is.EqualTo(5));
            Assert.That(group.ColorB, Is.EqualTo(6));
            Assert.That(group.Type, Is.EqualTo(GroupType.Closed));
        });
        var membership = (await _repository.GetMembershipAsync(groupId, 1))!;
        Assert.Multiple(() =>
        {
            Assert.That(membership.Rank, Is.EqualTo(GroupMemberRank.Admin));
            Assert.That(membership.IsPending, Is.False);
        });
    }

    [Test]
    public async Task UpdateInfoAsync_UpdatesNameAndDescription()
    {
        await SeedGroupAsync(1);

        await _repository.UpdateInfoAsync(1, "NewName", "NewDesc");

        var group = (await _repository.GetByIdAsync(1))!;
        Assert.Multiple(() =>
        {
            Assert.That(group.Name, Is.EqualTo("NewName"));
            Assert.That(group.Description, Is.EqualTo("NewDesc"));
        });
    }

    [Test]
    public async Task UpdateColorsAsync_UpdatesColors()
    {
        await SeedGroupAsync(1);

        await _repository.UpdateColorsAsync(1, 11, 12);

        var group = (await _repository.GetByIdAsync(1))!;
        Assert.Multiple(() =>
        {
            Assert.That(group.ColorA, Is.EqualTo(11));
            Assert.That(group.ColorB, Is.EqualTo(12));
        });
    }

    [Test]
    public async Task UpdatePreferencesAsync_UpdatesTypeAndDecoration()
    {
        await SeedGroupAsync(1);

        await _repository.UpdatePreferencesAsync(1, GroupType.Closed, true);

        var group = (await _repository.GetByIdAsync(1))!;
        Assert.Multiple(() =>
        {
            Assert.That(group.Type, Is.EqualTo(GroupType.Closed));
            Assert.That(group.AdminOnlyDecoration, Is.True);
        });
    }

    [Test]
    public async Task UpdateBadgeAsync_UpdatesBadge()
    {
        await SeedGroupAsync(1);

        await _repository.UpdateBadgeAsync(1, "newbadge");

        Assert.That((await _repository.GetByIdAsync(1))!.Badge, Is.EqualTo("newbadge"));
    }

    [Test]
    public async Task DeleteGroupAsync_RemovesGroupAndMemberships()
    {
        await SeedPlayerAsync(1, "a");
        await SeedGroupAsync(1);
        await SeedMembershipAsync(1, 1);

        await _repository.DeleteGroupAsync(1);
        Assert.Multiple(async () =>
        {
            Assert.That(await _repository.GetByIdAsync(1), Is.Null);
            Assert.That(await _repository.GetMembershipAsync(1, 1), Is.Null);
        });
    }

    [Test]
    public async Task AddMembershipAsync_AddsMembership()
    {
        await SeedPlayerAsync(1, "a");
        await SeedGroupAsync(1);

        await _repository.AddMembershipAsync(1, 1, GroupMemberRank.Member, true);

        var membership = (await _repository.GetMembershipAsync(1, 1))!;
        Assert.Multiple(() =>
        {
            Assert.That(membership.Rank, Is.EqualTo(GroupMemberRank.Member));
            Assert.That(membership.IsPending, Is.True);
        });
    }

    [Test]
    public async Task AddMembershipAsync_AlreadyExists_DoesNothing()
    {
        await SeedPlayerAsync(1, "a");
        await SeedGroupAsync(1);
        await _repository.AddMembershipAsync(1, 1, GroupMemberRank.Member, false);

        await _repository.AddMembershipAsync(1, 1, GroupMemberRank.Admin, true);

        var membership = (await _repository.GetMembershipAsync(1, 1))!;
        Assert.Multiple(() =>
        {
            Assert.That(membership.Rank, Is.EqualTo(GroupMemberRank.Member));
            Assert.That(membership.IsPending, Is.False);
        });
    }

    [Test]
    public async Task RemoveMembershipAsync_RemovesMembership()
    {
        await SeedPlayerAsync(1, "a");
        await SeedGroupAsync(1);
        await SeedMembershipAsync(1, 1);

        await _repository.RemoveMembershipAsync(1, 1);

        Assert.That(await _repository.GetMembershipAsync(1, 1), Is.Null);
    }

    [Test]
    public async Task SetPendingAsync_UpdatesPendingFlag()
    {
        await SeedPlayerAsync(1, "a");
        await SeedGroupAsync(1);
        await SeedMembershipAsync(1, 1, isPending: true);

        await _repository.SetPendingAsync(1, 1, false);

        Assert.That((await _repository.GetMembershipAsync(1, 1))!.IsPending, Is.False);
    }

    [Test]
    public async Task SetRankAsync_NonPendingMember_UpdatesRank()
    {
        await SeedPlayerAsync(1, "a");
        await SeedGroupAsync(1);
        await SeedMembershipAsync(1, 1);

        await _repository.SetRankAsync(1, 1, GroupMemberRank.Admin);

        Assert.That((await _repository.GetMembershipAsync(1, 1))!.Rank, Is.EqualTo(GroupMemberRank.Admin));
    }

    [Test]
    public async Task SetRankAsync_PendingMember_DoesNotUpdate()
    {
        await SeedPlayerAsync(1, "a");
        await SeedGroupAsync(1);
        await SeedMembershipAsync(1, 1, isPending: true);

        await _repository.SetRankAsync(1, 1, GroupMemberRank.Admin);

        Assert.That((await _repository.GetMembershipAsync(1, 1))!.Rank, Is.EqualTo(GroupMemberRank.Member));
    }
}
