using Ada.Core.Enums.Game.Moderation;
using Ada.Db.Models.Moderation;
using Ada.Db.Models.Players;
using Ada.Game.Moderation;
using Ada.Tests.Common;

namespace Ada.Tests.Game.Moderation;

[TestFixture]
public class ModerationTicketServiceTests
{
    private static readonly DateTimeOffset _baseTime = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private SqliteTestDbFactory _factory = null!;
    private ModerationTicketService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new SqliteTestDbFactory();
        _service = new ModerationTicketService(_factory);

        using var db = _factory.CreateDbContext();
        db.Players.Add(NewPlayer(1, "alice"));
        db.Players.Add(NewPlayer(2, "bob"));
        db.Players.Add(NewPlayer(3, "mod"));
        db.SaveChanges();
    }

    [TearDown]
    public void TearDown() => _factory.Dispose();

    private static Player NewPlayer(long id, string username) => new()
    {
        Id = id,
        Username = username,
        Email = $"{username}@test.local",
        Password = "secret"
    };

    private void SeedTicket(
        int id,
        ModerationTicketState state,
        long reporterId = 1,
        long? reportedId = null,
        long? pickedById = null,
        DateTimeOffset? createdAt = null)
    {
        using var db = _factory.CreateDbContext();
        db.ModerationTickets.Add(new ModerationTicket
        {
            Id = id,
            ReporterPlayerId = reporterId,
            ReportedPlayerId = reportedId,
            CategoryId = 7,
            Message = $"report {id}",
            State = state,
            PickedByPlayerId = pickedById,
            CreatedAt = createdAt ?? _baseTime,
            PickedAt = pickedById == null ? null : _baseTime.AddMinutes(1)
        });
        db.SaveChanges();
    }

    private ModerationTicket TicketRow(int id)
    {
        using var db = _factory.CreateDbContext();
        return db.ModerationTickets.Single(x => x.Id == id);
    }

    [Test]
    public async Task LoadAsync_HydratesOpenAndPickedTickets()
    {
        SeedTicket(1, ModerationTicketState.Open);
        SeedTicket(2, ModerationTicketState.Picked, reporterId: 2, reportedId: 1, pickedById: 3);

        await _service.LoadAsync();

        Assert.That(_service.GetActiveTickets(), Has.Count.EqualTo(2));

        var picked = _service.GetById(2)!;
        Assert.Multiple(() =>
        {
            Assert.That(picked.ReporterPlayerId, Is.EqualTo(2));
            Assert.That(picked.ReporterUsername, Is.EqualTo("bob"));
            Assert.That(picked.ReportedPlayerId, Is.EqualTo(1));
            Assert.That(picked.ReportedUsername, Is.EqualTo("alice"));
            Assert.That(picked.PickedByPlayerId, Is.EqualTo(3));
            Assert.That(picked.PickedByUsername, Is.EqualTo("mod"));
            Assert.That(picked.State, Is.EqualTo(ModerationTicketState.Picked));
            Assert.That(picked.PickedAt, Is.Not.Null);
            Assert.That(picked.CategoryId, Is.EqualTo(7));
            Assert.That(picked.Message, Is.EqualTo("report 2"));
        });
    }

    [Test]
    public async Task LoadAsync_NullNavigations_YieldNullUsernames()
    {
        SeedTicket(1, ModerationTicketState.Open);

        await _service.LoadAsync();

        var ticket = _service.GetById(1)!;
        Assert.Multiple(() =>
        {
            Assert.That(ticket.ReporterUsername, Is.EqualTo("alice"));
            Assert.That(ticket.ReportedUsername, Is.Null);
            Assert.That(ticket.PickedByUsername, Is.Null);
        });
    }

    [Test]
    public async Task LoadAsync_ExcludesClosedTickets()
    {
        SeedTicket(1, ModerationTicketState.Open);
        SeedTicket(2, ModerationTicketState.Closed, reporterId: 2);

        await _service.LoadAsync();
        Assert.Multiple(() =>
        {
            Assert.That(_service.GetById(2), Is.Null);
            Assert.That(_service.GetActiveTickets(), Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task GetActiveTickets_OrdersByCreatedAt()
    {
        SeedTicket(1, ModerationTicketState.Open, createdAt: _baseTime.AddHours(2));
        SeedTicket(2, ModerationTicketState.Open, reporterId: 2, createdAt: _baseTime);

        await _service.LoadAsync();

        Assert.That(_service.GetActiveTickets().Select(x => x.Id), Is.EqualTo(new[] { 2, 1 }));
    }

    [Test]
    public void GetById_Unknown_ReturnsNull()
    {
        Assert.That(_service.GetById(404), Is.Null);
    }

    [Test]
    public async Task CreateAsync_PersistsAndCaches()
    {
        var dto = await _service.CreateAsync(1, 2, null, 5, "bad words");

        Assert.That(dto, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(dto!.Id, Is.GreaterThan(0));
            Assert.That(dto.ReporterUsername, Is.EqualTo("alice"));
            Assert.That(dto.ReportedUsername, Is.EqualTo("bob"));
            Assert.That(dto.CategoryId, Is.EqualTo(5));
            Assert.That(dto.Message, Is.EqualTo("bad words"));
            Assert.That(dto.State, Is.EqualTo(ModerationTicketState.Open));
            Assert.That(dto.Resolution, Is.EqualTo(ModerationTicketResolution.None));
            Assert.That(_service.GetById(dto.Id), Is.SameAs(dto));
        });
        var row = TicketRow(dto.Id);
        Assert.Multiple(() =>
        {
            Assert.That(row.State, Is.EqualTo(ModerationTicketState.Open));
            Assert.That(row.ReporterPlayerId, Is.EqualTo(1));
            Assert.That(row.ReportedPlayerId, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task CreateAsync_NullReported_EmptyReportedUsername()
    {
        var dto = await _service.CreateAsync(1, null, null, 5, "spam");
        Assert.Multiple(() =>
        {
            Assert.That(dto!.ReportedPlayerId, Is.Null);
            Assert.That(dto.ReportedUsername, Is.Empty);
        });
    }

    [Test]
    public async Task CreateAsync_ReporterHasActiveTicket_ReturnsNull()
    {
        await _service.CreateAsync(1, null, null, 5, "first");

        Assert.That(await _service.CreateAsync(1, null, null, 5, "second"), Is.Null);
    }

    [Test]
    public async Task CreateAsync_AfterClose_AllowsNewTicket()
    {
        var first = await _service.CreateAsync(1, null, null, 5, "first");
        await _service.TryCloseAsync(first!.Id, 3, ModerationTicketResolution.Resolved);

        Assert.That(await _service.CreateAsync(1, null, null, 5, "second"), Is.Not.Null);
    }

    [Test]
    public async Task TryPickAsync_OpenTicket_PicksAndPersists()
    {
        var dto = await _service.CreateAsync(1, null, null, 5, "report");

        var picked = await _service.TryPickAsync(dto!.Id, 3, "mod");

        Assert.That(picked, Is.SameAs(dto));
        Assert.Multiple(() =>
        {
            Assert.That(picked!.State, Is.EqualTo(ModerationTicketState.Picked));
            Assert.That(picked.PickedByPlayerId, Is.EqualTo(3));
            Assert.That(picked.PickedByUsername, Is.EqualTo("mod"));
            Assert.That(picked.PickedAt, Is.Not.Null);
        });
        var row = TicketRow(dto.Id);
        Assert.Multiple(() =>
        {
            Assert.That(row.State, Is.EqualTo(ModerationTicketState.Picked));
            Assert.That(row.PickedByPlayerId, Is.EqualTo(3));
            Assert.That(row.PickedAt, Is.Not.Null);
        });
    }

    [Test]
    public async Task TryPickAsync_Unknown_ReturnsNull()
    {
        Assert.That(await _service.TryPickAsync(404, 3, "mod"), Is.Null);
    }

    [Test]
    public async Task TryPickAsync_AlreadyPicked_ReturnsNull()
    {
        var dto = await _service.CreateAsync(1, null, null, 5, "report");
        await _service.TryPickAsync(dto!.Id, 3, "mod");

        Assert.That(await _service.TryPickAsync(dto.Id, 2, "bob"), Is.Null);
    }

    [Test]
    public async Task TryPickAsync_DbRowGone_ReturnsNull()
    {
        var dto = await _service.CreateAsync(1, null, null, 5, "report");

        using (var db = _factory.CreateDbContext())
        {
            db.ModerationTickets.Remove(db.ModerationTickets.Single(x => x.Id == dto!.Id));
            db.SaveChanges();
        }

        Assert.Multiple(async () =>
        {
            Assert.That(await _service.TryPickAsync(dto!.Id, 3, "mod"), Is.Null);
            Assert.That(dto.State, Is.EqualTo(ModerationTicketState.Open));
        });
    }

    [Test]
    public async Task TryReleaseAsync_ByPicker_ReopensAndPersists()
    {
        var dto = await _service.CreateAsync(1, null, null, 5, "report");
        await _service.TryPickAsync(dto!.Id, 3, "mod");

        var released = await _service.TryReleaseAsync(dto.Id, 3);

        Assert.That(released, Is.SameAs(dto));
        Assert.Multiple(() =>
        {
            Assert.That(released!.State, Is.EqualTo(ModerationTicketState.Open));
            Assert.That(released.PickedByPlayerId, Is.Null);
            Assert.That(released.PickedByUsername, Is.Empty);
            Assert.That(released.PickedAt, Is.Null);
        });
        var row = TicketRow(dto.Id);
        Assert.Multiple(() =>
        {
            Assert.That(row.State, Is.EqualTo(ModerationTicketState.Open));
            Assert.That(row.PickedByPlayerId, Is.Null);
            Assert.That(row.PickedAt, Is.Null);
        });
    }

    [Test]
    public async Task TryReleaseAsync_Unknown_ReturnsNull()
    {
        Assert.That(await _service.TryReleaseAsync(404, 3), Is.Null);
    }

    [Test]
    public async Task TryReleaseAsync_NotPicked_ReturnsNull()
    {
        var dto = await _service.CreateAsync(1, null, null, 5, "report");

        Assert.That(await _service.TryReleaseAsync(dto!.Id, 3), Is.Null);
    }

    [Test]
    public async Task TryReleaseAsync_DifferentModerator_ReturnsNull()
    {
        var dto = await _service.CreateAsync(1, null, null, 5, "report");
        await _service.TryPickAsync(dto!.Id, 3, "mod");
        Assert.Multiple(async () =>
        {
            Assert.That(await _service.TryReleaseAsync(dto.Id, 2), Is.Null);
            Assert.That(dto.State, Is.EqualTo(ModerationTicketState.Picked));
        });
    }

    [TestCase(ModerationTicketResolution.Useless)]
    [TestCase(ModerationTicketResolution.Abusive)]
    [TestCase(ModerationTicketResolution.Resolved)]
    public async Task TryCloseAsync_ClosesPersistsAndEvicts(ModerationTicketResolution resolution)
    {
        var dto = await _service.CreateAsync(1, null, null, 5, "report");
        await _service.TryPickAsync(dto!.Id, 3, "mod");

        var closed = await _service.TryCloseAsync(dto.Id, 3, resolution);

        Assert.That(closed, Is.SameAs(dto));
        Assert.Multiple(() =>
        {
            Assert.That(closed!.State, Is.EqualTo(ModerationTicketState.Closed));
            Assert.That(closed.Resolution, Is.EqualTo(resolution));
            Assert.That(closed.PickedByPlayerId, Is.EqualTo(3));
            Assert.That(closed.ClosedAt, Is.Not.Null);
            Assert.That(_service.GetById(dto.Id), Is.Null);
            Assert.That(_service.GetActiveTickets(), Is.Empty);
        });
        var row = TicketRow(dto.Id);
        Assert.Multiple(() =>
        {
            Assert.That(row.State, Is.EqualTo(ModerationTicketState.Closed));
            Assert.That(row.Resolution, Is.EqualTo(resolution));
            Assert.That(row.PickedByPlayerId, Is.EqualTo(3));
            Assert.That(row.ClosedAt, Is.Not.Null);
        });
    }

    [Test]
    public async Task TryCloseAsync_OpenTicket_ClosesWithoutPick()
    {
        var dto = await _service.CreateAsync(1, null, null, 5, "report");

        var closed = await _service.TryCloseAsync(dto!.Id, 3, ModerationTicketResolution.Useless);

        Assert.That(closed, Is.Not.Null);
        Assert.That(closed!.State, Is.EqualTo(ModerationTicketState.Closed));
    }

    [Test]
    public async Task TryCloseAsync_TicketPickedByAnotherModerator_ReturnsNullAndLeavesItOpen()
    {
        var dto = await _service.CreateAsync(1, null, null, 5, "report");
        await _service.TryPickAsync(dto!.Id, 3, "mod");

        var closed = await _service.TryCloseAsync(dto.Id, 99, ModerationTicketResolution.Useless);
        Assert.Multiple(() =>
        {
            Assert.That(closed, Is.Null);
            Assert.That(_service.GetById(dto.Id), Is.Not.Null);
        });
        var row = TicketRow(dto.Id);
        Assert.Multiple(() =>
        {
            Assert.That(row.State, Is.EqualTo(ModerationTicketState.Picked));
            Assert.That(row.PickedByPlayerId, Is.EqualTo(3), "attribution must not be stolen by the closer");
        });
    }

    [Test]
    public async Task TryCloseAsync_TicketPickedBySelf_Closes()
    {
        var dto = await _service.CreateAsync(1, null, null, 5, "report");
        await _service.TryPickAsync(dto!.Id, 3, "mod");

        var closed = await _service.TryCloseAsync(dto.Id, 3, ModerationTicketResolution.Resolved);
        Assert.Multiple(() =>
        {
            Assert.That(closed, Is.Not.Null);
            Assert.That(TicketRow(dto.Id).PickedByPlayerId, Is.EqualTo(3));
        });
    }

    [Test]
    public async Task TryCloseAsync_Unknown_ReturnsNull()
    {
        Assert.That(await _service.TryCloseAsync(404, 3, ModerationTicketResolution.Useless), Is.Null);
    }

    [Test]
    public async Task TryCloseAsync_AlreadyClosedInCache_ReturnsNull()
    {
        var dto = await _service.CreateAsync(1, null, null, 5, "report");
        dto!.State = ModerationTicketState.Closed;

        Assert.That(await _service.TryCloseAsync(dto.Id, 3, ModerationTicketResolution.Useless), Is.Null);
    }

    [Test]
    public async Task GetTopicsAsync_ReturnsSeededTopicsOrdered()
    {
        var topics = await _service.GetTopicsAsync();

        Assert.That(topics, Has.Count.EqualTo(12));
        Assert.Multiple(() =>
        {
            Assert.That(topics[0].Name, Is.EqualTo("Verbal abuse"));
            Assert.That(topics[0].CategoryName, Is.EqualTo("Bullying"));
            Assert.That(topics.Select(x => x.Order), Is.Ordered.Ascending);
        });
    }

    [Test]
    public async Task GetTopicsAsync_CachesAfterFirstCall()
    {
        var first = await _service.GetTopicsAsync();

        using (var db = _factory.CreateDbContext())
        {
            db.ModerationCfhTopics.RemoveRange(db.ModerationCfhTopics.ToList());
            db.SaveChanges();
        }

        Assert.That(await _service.GetTopicsAsync(), Is.SameAs(first));
    }
}
