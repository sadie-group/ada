using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Networking.Client;
using Ada.Networking.Events.Handlers.Players.Wardrobe;
using Ada.Tests.Common;
using AutoMapper;
using Moq;

namespace Ada.Tests.Networking.Events.Handlers.Players;

[TestFixture]
public class PlayerWardrobeSaveEventHandlerTests
{
    private const long _playerId = 1;

    private SqliteTestDbFactory _dbFactory = null!;
    private ICollection<PlayerWardrobeItemDto> _wardrobe = null!;
    private INetworkClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _dbFactory = new SqliteTestDbFactory();
        _wardrobe = new List<PlayerWardrobeItemDto>();

        using (var db = _dbFactory.CreateDbContext())
        {
            db.Players.Add(new global::Ada.Db.Models.Players.Player
            {
                Id = _playerId,
                Username = "player",
                Email = "",
                Password = ""
            });

            db.SaveChanges();
        }

        var player = new Mock<IPlayerLogic>();
        player.SetupGet(x => x.Player).Returns(MakePlayerDto(_wardrobe));

        var client = new Mock<INetworkClient>();
        client.SetupGet(x => x.Player).Returns(player.Object);

        _client = client.Object;
    }

    [TearDown]
    public void TearDown() => _dbFactory.Dispose();

    private PlayerWardrobeSaveEventHandler Handler(int slotId, string figureCode) =>
        new(_dbFactory, MapperStub())
        {
            SlotId = slotId,
            FigureCode = figureCode,
            Gender = "M"
        };

    private async Task<int> StoredRowCountAsync()
    {
        await using var db = _dbFactory.CreateDbContext();
        return await db.PlayerWardrobeItems.CountAsync();
    }

    [Test]
    public async Task Save_SameSlotTwice_ReplacesRatherThanAccumulating()
    {
        await Handler(3, "hd-180-1").HandleAsync(_client);
        await Handler(3, "hr-100-61.hd-180-1").HandleAsync(_client);

        Assert.Multiple(async () =>
        {
            Assert.That(await StoredRowCountAsync(), Is.EqualTo(1),
                "re-saving a slot used to insert another row every time");
            Assert.That(_wardrobe, Has.Exactly(1).Items);
            Assert.That(_wardrobe.Single().FigureCode, Is.EqualTo("hr-100-61.hd-180-1"));
        });
    }

    [Test]
    public async Task Save_DifferentSlots_AreKeptSeparately()
    {
        await Handler(0, "hd-180-1").HandleAsync(_client);
        await Handler(1, "hd-180-2").HandleAsync(_client);

        Assert.That(await StoredRowCountAsync(), Is.EqualTo(2));
    }

    [TestCase(-1)]
    [TestCase(10)]
    [TestCase(99999)]
    public async Task Save_SlotOutsideRange_IsRejected(int slotId)
    {
        await Handler(slotId, "hd-180-1").HandleAsync(_client);

        Assert.That(await StoredRowCountAsync(), Is.Zero);
    }

    [TestCase("")]
    [TestCase("<script>alert(1)</script>")]
    [TestCase("not a figure")]
    public async Task Save_InvalidFigureCode_IsRejected(string figureCode)
    {
        await Handler(0, figureCode).HandleAsync(_client);

        Assert.That(await StoredRowCountAsync(), Is.Zero);
    }

    private static IMapper MapperStub()
    {
        var mapper = new Mock<IMapper>();

        mapper.Setup(x => x.Map<PlayerWardrobeItemDto>(It.IsAny<object>()))
            .Returns((object source) =>
            {
                var item = (global::Ada.Db.Models.Players.PlayerWardrobeItem)source;

                return new PlayerWardrobeItemDto
                {
                    Id = item.Id,
                    SlotId = item.SlotId,
                    FigureCode = item.FigureCode,
                    Gender = item.Gender
                };
            });

        return mapper.Object;
    }

    private static PlayerDto MakePlayerDto(ICollection<PlayerWardrobeItemDto> wardrobe) => new(
        _playerId, "player", "", DateTimeOffset.UtcNow, [], new PlayerDataDto(),
        new PlayerAvatarDataDto { FigureCode = "hd-180-1", Motto = "" }, [], [], [], [],
        new PlayerNavigatorSettingsDto(), new PlayerGameSettingsDto(), [], [],
        wardrobe, [], [], [], [], [], [], [], [], [], [], [], [], [], []);
}
