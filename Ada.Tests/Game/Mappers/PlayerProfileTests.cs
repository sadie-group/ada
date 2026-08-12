using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Game.Players;
using Ada.Db.Models.Players;
using Ada.Game.Mappers;
using Ada.Game.Players;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ada.Tests.Game.Mappers;

[TestFixture]
public class PlayerProfileTests
{
    private static IMapper CreateMapper()
    {
        var provider = new ServiceCollection()
            .AddSingleton<ILogger<PlayerLogic>>(NullLogger<PlayerLogic>.Instance)
            .BuildServiceProvider();

        var configuration = new MapperConfiguration(
            cfg =>
            {
                cfg.AddProfile(new PlayerProfile(provider));
                cfg.ShouldMapProperty = p => p.GetIndexParameters().Length == 0;
            },
            NullLoggerFactory.Instance);

        return configuration.CreateMapper();
    }

    private static PlayerDto CreatePlayerDto(long id = 1, string username = "alice")
    {
        return new PlayerDto(
            id,
            username,
            "alice@test.com",
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            [],
            new PlayerDataDto(),
            new PlayerAvatarDataDto { FigureCode = "fig", Motto = "motto" },
            [],
            [],
            [],
            [],
            new PlayerNavigatorSettingsDto(),
            new PlayerGameSettingsDto(),
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            []);
    }

    [Test]
    public void Map_PlayerDtoToPlayerLogic_ConstructsLogicAroundDto()
    {
        var mapper = CreateMapper();
        var dto = CreatePlayerDto();

        var logic = mapper.Map<IPlayerLogic>(dto);

        Assert.Multiple(() =>
        {
            Assert.That(logic, Is.InstanceOf<PlayerLogic>());
            Assert.That(logic.Player, Is.SameAs(dto));
            Assert.That(logic.NetworkObject, Is.Null);
        });
    }

    [Test]
    public void Map_PlayerToPlayerDto_MapsScalarsAndAvatarData()
    {
        var mapper = CreateMapper();
        var player = new Player
        {
            Id = 9,
            Username = "bob",
            Email = "bob@test.com",
            Password = "secret",
            CreatedAt = new DateTimeOffset(2026, 2, 3, 0, 0, 0, TimeSpan.Zero),
            AvatarData = new PlayerAvatarData { FigureCode = "fig-b" }
        };

        var dto = mapper.Map<PlayerDto>(player);

        Assert.Multiple(() =>
        {
            Assert.That(dto.Id, Is.EqualTo(9));
            Assert.That(dto.Username, Is.EqualTo("bob"));
            Assert.That(dto.Email, Is.EqualTo("bob@test.com"));
            Assert.That(dto.CreatedAt, Is.EqualTo(player.CreatedAt));
            Assert.That(dto.AvatarData!.FigureCode, Is.EqualTo("fig-b"));
            Assert.That(dto.Roles, Is.Empty);
        });
    }

    [Test]
    public void Map_PlayerDataDto_ReverseMap_RoundTrips()
    {
        var mapper = CreateMapper();
        var dto = new PlayerDataDto
        {
            CreditBalance = 100,
            PixelBalance = 50,
            IsOnline = true
        };

        var entity = mapper.Map<PlayerData>(dto);
        var roundTripped = mapper.Map<PlayerDataDto>(entity);

        Assert.Multiple(() =>
        {
            Assert.That(entity.CreditBalance, Is.EqualTo(100));
            Assert.That(entity.PixelBalance, Is.EqualTo(50));
            Assert.That(entity.IsOnline, Is.True);
            Assert.That(roundTripped.CreditBalance, Is.EqualTo(100));
            Assert.That(roundTripped.IsOnline, Is.True);
        });
    }
}
