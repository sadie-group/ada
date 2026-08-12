using Ada.API.DTOs.Furniture;
using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking;
using Ada.Core.Enums.Game.Furniture;
using Ada.Core.Enums.Miscellaneous;
using Ada.Db.Models.Furniture;
using Ada.Db.Models.Players;
using Ada.Db.Models.Players.Furniture;
using Ada.Game.Rooms.Furniture;
using Ada.Networking.Writers.Rooms.Furniture;
using Ada.Tests.Common;
using AutoMapper;
using Moq;

namespace Ada.Tests.Game.Rooms.Furniture;

[TestFixture]
public class RoomFurnitureItemHelperServiceTests
{
    private static PlayerFurnitureItemPlacementDataDto MakeItem(
        int id,
        string metaData,
        int interactionModes,
        string? interactionType = null)
        => new()
        {
            Id = id,
            PlayerFurnitureItemId = id,
            PlayerFurnitureItem = new PlayerFurnitureItemDto
            {
                Id = id,
                FurnitureItemId = 1,
                LimitedData = "",
                MetaData = metaData,
                FurnitureItem = new FurnitureItemDto
                {
                    Name = "",
                    AssetName = "",
                    InteractionType = interactionType,
                    InteractionModes = interactionModes,
                    Type = FurnitureItemType.Floor
                }
            }
        };

    private static (Mock<IRoomLogic> Room, List<AbstractPacketWriter> Written) MakeRoom()
    {
        var written = new List<AbstractPacketWriter>();
        var room = new Mock<IRoomLogic>();

        room.Setup(x => x.BroadcastDataAsync(It.IsAny<AbstractPacketWriter>(), It.IsAny<IReadOnlyCollection<long>?>()))
            .Callback<AbstractPacketWriter, IReadOnlyCollection<long>?>((writer, _) => written.Add(writer))
            .Returns(Task.CompletedTask);

        return (room, written);
    }

    private static RoomFurnitureItemHelperService CreateService(SqliteTestDbFactory factory)
        => new(factory, Mock.Of<IPlayerRepository>());

    private static SqliteTestDbFactory CreateSeededFactory(string metaData = "0")
    {
        var factory = new SqliteTestDbFactory();

        using var db = factory.CreateDbContext();

        var player = new Player { Id = 1, Username = "u", Email = "e", Password = "p" };
        var furnitureItem = new FurnitureItem
        {
            Id = 1,
            Name = "item",
            AssetName = "item",
            InteractionType = null
        };

        db.Players.Add(player);
        db.FurnitureItems.Add(furnitureItem);
        db.PlayerFurnitureItems.Add(new PlayerFurnitureItem
        {
            Id = 10,
            Player = player,
            FurnitureItemId = 1,
            FurnitureItem = furnitureItem,
            LimitedData = "",
            MetaData = metaData
        });

        db.SaveChanges();

        return factory;
    }

    [Test]
    public async Task CycleInteractionState_NoInteractionModes_DoesNothing()
    {
        using var factory = new SqliteTestDbFactory();
        var service = CreateService(factory);
        var (room, written) = MakeRoom();
        var item = MakeItem(10, "", 0);

        await service.CycleInteractionStateForItemAsync(room.Object, item);

        Assert.Multiple(() =>
        {
            Assert.That(item.PlayerFurnitureItem.MetaData, Is.EqualTo("0"));
            Assert.That(written, Is.Empty);
        });
    }

    [Test]
    public async Task CycleInteractionState_UnparsableMetaData_DoesNothing()
    {
        using var factory = new SqliteTestDbFactory();
        var service = CreateService(factory);
        var (room, written) = MakeRoom();
        var item = MakeItem(10, "abc", 2);

        await service.CycleInteractionStateForItemAsync(room.Object, item);

        Assert.Multiple(() =>
        {
            Assert.That(item.PlayerFurnitureItem.MetaData, Is.EqualTo("abc"));
            Assert.That(written, Is.Empty);
        });
    }

    [Test]
    public async Task CycleInteractionState_EmptyMetaData_CyclesToOneAndPersists()
    {
        using var factory = CreateSeededFactory();
        var service = CreateService(factory);
        var (room, written) = MakeRoom();
        var item = MakeItem(10, "", 2);

        await service.CycleInteractionStateForItemAsync(room.Object, item);

        await using var db = factory.CreateDbContext();

        Assert.Multiple(() =>
        {
            Assert.That(item.PlayerFurnitureItem.MetaData, Is.EqualTo("1"));
            Assert.That(written, Has.Count.EqualTo(1));
            Assert.That(written[0], Is.InstanceOf<RoomFloorItemUpdatedWriter>());
            Assert.That(((RoomFloorItemUpdatedWriter)written[0]).MetaData, Is.EqualTo("1"));
            Assert.That(db.PlayerFurnitureItems.Single(x => x.Id == 10).MetaData, Is.EqualTo("1"));
        });
    }

    [Test]
    public async Task CycleInteractionState_StateAtMax_WrapsBackToOne()
    {
        using var factory = CreateSeededFactory("5");
        var service = CreateService(factory);
        var (room, _) = MakeRoom();
        var item = MakeItem(10, "5", 2);

        await service.CycleInteractionStateForItemAsync(room.Object, item);

        await using var db = factory.CreateDbContext();

        Assert.Multiple(() =>
        {
            Assert.That(item.PlayerFurnitureItem.MetaData, Is.EqualTo("1"));
            Assert.That(db.PlayerFurnitureItems.Single(x => x.Id == 10).MetaData, Is.EqualTo("1"));
        });
    }

    [Test]
    public async Task CycleInteractionState_MidCycle_AdvancesState()
    {
        using var factory = CreateSeededFactory("1");
        var service = CreateService(factory);
        var (room, _) = MakeRoom();
        var item = MakeItem(10, "1", 3);

        await service.CycleInteractionStateForItemAsync(room.Object, item);

        Assert.That(item.PlayerFurnitureItem.MetaData, Is.EqualTo("2"));
    }

    [Test]
    public void GetObjectDataKeyForItem_RoomAdsBg_ReturnsMapKey()
    {
        using var factory = new SqliteTestDbFactory();
        var service = CreateService(factory);
        var item = MakeItem(1, "", 0, FurnitureItemInteractionType.RoomAdsBg);

        Assert.That(service.GetObjectDataKeyForItem(item), Is.EqualTo(ObjectDataKey.MapKey));
    }

    [Test]
    public void GetObjectDataKeyForItem_Other_ReturnsLegacyKey()
    {
        using var factory = new SqliteTestDbFactory();
        var service = CreateService(factory);
        var item = MakeItem(1, "", 0);

        Assert.That(service.GetObjectDataKeyForItem(item), Is.EqualTo(ObjectDataKey.LegacyKey));
    }

    [Test]
    public void GetObjectDataForItem_NonAdsItem_ReturnsEmpty()
    {
        using var factory = new SqliteTestDbFactory();
        var service = CreateService(factory);
        var item = MakeItem(1, "state=1", 0);

        Assert.That(service.GetObjectDataForItem(item), Is.Empty);
    }

    [Test]
    public void GetObjectDataForItem_AdsItem_ParsesKeyValuePairs()
    {
        using var factory = new SqliteTestDbFactory();
        var service = CreateService(factory);
        var item = MakeItem(1, "state=1;imageUrl=http://x;plain", 0, FurnitureItemInteractionType.RoomAdsBg);

        var data = service.GetObjectDataForItem(item);

        Assert.Multiple(() =>
        {
            Assert.That(data["state"], Is.EqualTo("1"));
            Assert.That(data["imageUrl"], Is.EqualTo("http://x"));
            Assert.That(data["plain"], Is.EqualTo(""));
        });
    }
}
