using Ada.API.Collections;
using Ada.API.DTOs.Players.Furniture;
using Ada.API.DTOs.Rooms;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Bots;
using Ada.API.Interfaces.Game.Rooms.Pets;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Db.Models.Furniture;
using Ada.Db.Models.Players.Furniture;
using Ada.Db.Models.Rooms;
using Ada.Game.Mappers;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Ada.Tests.Game.Mappers;

[TestFixture]
public class RoomProfileTests
{
    private static IMapper CreateMapper()
    {
        var provider = new ServiceCollection()
            .AddSingleton(Mock.Of<IRoomUserRepository>())
            .AddSingleton(Mock.Of<IRoomBotRepository>())
            .AddSingleton(Mock.Of<IRoomPetRepository>())
            .BuildServiceProvider();

        var configuration = new MapperConfiguration(
            cfg =>
            {
                cfg.AddProfile(new RoomProfile(provider));

                cfg.AddProfile(new FurnitureItemProfile());
                cfg.ShouldMapProperty = p => p.GetIndexParameters().Length == 0;
            },
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        return configuration.CreateMapper();
    }

    [Test]
    public void Map_RoomDtoToRoomLogic_BuildsTileMapAndPathFinder()
    {
        var mapper = CreateMapper();
        var dto = new RoomDto
        {
            Name = "test room",
            Description = "desc",
            Layout = new RoomLayoutDto { Heightmap = "00\n00" },
            FurnitureItems = [],
        };

        var logic = mapper.Map<IRoomLogic>(dto);

        Assert.Multiple(() =>
        {
            Assert.That(logic.Room, Is.SameAs(dto));
            Assert.That(logic.TileMap.SizeX, Is.EqualTo(2));
            Assert.That(logic.TileMap.SizeY, Is.EqualTo(2));
            Assert.That(logic.PathFinder, Is.Not.Null);
        });
    }

    [Test]
    public void Map_RoomModelToDto_RoundTrips()
    {
        var mapper = CreateMapper();
        var room = new Room { Name = "lobby", Description = "the lobby" };

        var dto = mapper.Map<RoomDto>(room);

        Assert.Multiple(() =>
        {
            Assert.That(dto.Name, Is.EqualTo("lobby"));
            Assert.That(dto.Description, Is.EqualTo("the lobby"));
        });
    }

    [Test]
    public void Map_RoomModelToDto_KeepsRevisionedFurnitureCollection()
    {
        var mapper = CreateMapper();

        var dto = mapper.Map<RoomDto>(new Room { Name = "lobby", Description = "the lobby" });

        Assert.That(dto.FurnitureItems,
            Is.InstanceOf<RevisionedCollection<PlayerFurnitureItemPlacementDataDto>>());
    }

    [Test]
    public void Map_RoomModelToDto_TransfersFurnitureItemsIntoTheRevisionedCollection()
    {
        var mapper = CreateMapper();

        var room = new Room { Name = "lobby", Description = "the lobby" };
        room.FurnitureItems.Add(PlacementEntity(1, 3, 4));
        room.FurnitureItems.Add(PlacementEntity(2, 5, 6));

        var dto = mapper.Map<RoomDto>(room);

        Assert.Multiple(() =>
        {
            Assert.That(dto.FurnitureItems, Has.Count.EqualTo(2));
            Assert.That(dto.FurnitureItems.Select(x => (x.PositionX, x.PositionY)),
                Is.EquivalentTo(new[] { (3, 4), (5, 6) }));

            Assert.That(dto.FurnitureItems.Revision, Is.EqualTo(1));
        });
    }

    private static PlayerFurnitureItemPlacementData PlacementEntity(int id, int x, int y) => new()
    {
        Id = id,
        PlayerFurnitureItemId = id,
        PlayerFurnitureItem = new PlayerFurnitureItem
        {
            Id = id,
            LimitedData = "",
            MetaData = "",
            Player = null!,
            FurnitureItemId = 0,
            FurnitureItem = new FurnitureItem
            {
                Name = "chair",
                AssetName = "chair",
                InteractionType = "default"
            }
        },
        PositionX = x,
        PositionY = y
    };
}
