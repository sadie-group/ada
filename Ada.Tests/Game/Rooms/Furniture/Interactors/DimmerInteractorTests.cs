using Ada.API.DTOs.Furniture;
using Ada.API.DTOs.Players.Furniture;
using Ada.API.DTOs.Rooms;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Core.Enums.Game.Furniture;
using Ada.Db.Models.Players;
using Ada.Db.Models.Rooms;
using Ada.Game.Rooms.Furniture.Interactors;
using Ada.Tests.Common;
using AutoMapper;
using Moq;

namespace Ada.Tests.Game.Rooms.Furniture.Interactors;

[TestFixture]
public class DimmerInteractorTests
{
    private static DimmerInteractor MakeInteractor(SqliteTestDbFactory factory)
    {
        var mapper = new Mock<IMapper>();
        mapper.Setup(x => x.Map<RoomDimmerSettings>(It.IsAny<object>()))
            .Returns((object source) =>
            {
                var dto = (RoomDimmerSettingsDto)source;
                return new RoomDimmerSettings { RoomId = dto.RoomId, Enabled = dto.Enabled, PresetId = dto.PresetId };
            });

        return new DimmerInteractor(factory, mapper.Object);
    }

    private static (Mock<IRoomLogic> Room, RoomDto Dto) MakeRoom(int roomId, RoomDimmerSettingsDto? settings)
    {
        var dto = new RoomDto { Id = roomId, DimmerSettings = settings };
        var room = new Mock<IRoomLogic>();
        room.SetupGet(x => x.Room).Returns(dto);
        return (room, dto);
    }

    private static PlayerFurnitureItemPlacementDataDto MakeItem()
    {
        return new PlayerFurnitureItemPlacementDataDto
        {
            Id = 1,
            PlayerFurnitureItemId = 1,
            PlayerFurnitureItem = new PlayerFurnitureItemDto
            {
                FurnitureItem = new FurnitureItemDto
                {
                    Name = "dimmer",
                    AssetName = "dimmer",
                    InteractionType = FurnitureItemInteractionType.Dimmer
                },
                FurnitureItemId = 1,
                LimitedData = "",
                MetaData = ""
            }
        };
    }

    private static async Task SeedRoomAsync(SqliteTestDbFactory factory, int roomId)
    {
        await using var db = factory.CreateDbContext();
        db.Players.Add(new Player { Id = 1000 + roomId, Username = $"owner{roomId}", Email = "e", Password = "p" });
        db.RoomLayouts.Add(new RoomLayout { Id = 1 });
        db.Rooms.Add(new Room { Id = roomId, Name = $"Room{roomId}", Description = "", OwnerId = 1000 + roomId, LayoutId = 1 });
        await db.SaveChangesAsync();
    }

    [Test]
    public void InteractionTypes_ContainsDimmer()
    {
        using var factory = new SqliteTestDbFactory();

        Assert.That(MakeInteractor(factory).InteractionTypes, Is.EqualTo(new[] { FurnitureItemInteractionType.Dimmer }));
    }

    [Test]
    public async Task OnPlaceAsync_NoExistingSettings_CreatesDefaultPresetsAndSettings()
    {
        using var factory = new SqliteTestDbFactory();
        await SeedRoomAsync(factory, 1);
        var (room, dto) = MakeRoom(1, null);

        await MakeInteractor(factory).OnPlaceAsync(room.Object, MakeItem(), Mock.Of<IRoomUser>());

        Assert.That(dto.DimmerSettings, Is.Not.Null);
        Assert.That(dto.DimmerSettings!.RoomId, Is.EqualTo(1));
        Assert.That(dto.DimmerSettings.Enabled, Is.False);
        Assert.That(dto.DimmerSettings.PresetId, Is.EqualTo(1));

        await using var db = factory.CreateDbContext();
        var presets = db.RoomDimmerPresets.Where(x => x.RoomId == 1).OrderBy(x => x.PresetId).ToList();
        Assert.That(presets.Select(x => x.PresetId), Is.EqualTo(new[] { 1, 2, 3 }));
        Assert.That(presets.All(x => x is { Intensity: 255, BackgroundOnly: false, Color: "" }), Is.True);
        Assert.That(db.RoomDimmerSettings.Count(x => x.RoomId == 1), Is.EqualTo(1));
    }

    [Test]
    public async Task OnPlaceAsync_SettingsAlreadyExist_WritesNothing()
    {
        using var factory = new SqliteTestDbFactory();
        var existing = new RoomDimmerSettingsDto { RoomId = 1, Enabled = true, PresetId = 2 };
        var (room, dto) = MakeRoom(1, existing);

        await MakeInteractor(factory).OnPlaceAsync(room.Object, MakeItem(), Mock.Of<IRoomUser>());

        Assert.That(dto.DimmerSettings, Is.SameAs(existing));

        await using var db = factory.CreateDbContext();
        Assert.That(db.RoomDimmerPresets.Count(), Is.Zero);
        Assert.That(db.RoomDimmerSettings.Count(), Is.Zero);
    }

    [Test]
    public async Task OnPickUpAsync_WithSettings_DeletesPresetsAndSettings()
    {
        using var factory = new SqliteTestDbFactory();
        await SeedRoomAsync(factory, 1);

        await using (var db = factory.CreateDbContext())
        {
            for (var presetId = 1; presetId <= 3; presetId++)
            {
                db.RoomDimmerPresets.Add(new RoomDimmerPreset
                {
                    RoomId = 1, PresetId = presetId, BackgroundOnly = false, Color = "", Intensity = 255
                });
            }

            db.RoomDimmerSettings.Add(new RoomDimmerSettings { RoomId = 1, Enabled = true, PresetId = 2 });
            await db.SaveChangesAsync();
        }

        var (room, dto) = MakeRoom(1, new RoomDimmerSettingsDto { RoomId = 1, Enabled = true, PresetId = 2 });

        await MakeInteractor(factory).OnPickUpAsync(room.Object, MakeItem(), Mock.Of<IRoomUser>());

        Assert.That(dto.DimmerSettings, Is.Null);

        await using var check = factory.CreateDbContext();
        Assert.That(check.RoomDimmerPresets.Count(), Is.Zero);
        Assert.That(check.RoomDimmerSettings.Count(), Is.Zero);
    }

    [Test]
    public async Task OnPickUpAsync_NoSettings_DeletesPresetsOnly()
    {
        using var factory = new SqliteTestDbFactory();

        await using (var db = factory.CreateDbContext())
        {
            db.RoomDimmerPresets.Add(new RoomDimmerPreset
            {
                RoomId = 1, PresetId = 1, BackgroundOnly = false, Color = "", Intensity = 255
            });
            await db.SaveChangesAsync();
        }

        var (room, dto) = MakeRoom(1, null);

        await MakeInteractor(factory).OnPickUpAsync(room.Object, MakeItem(), Mock.Of<IRoomUser>());

        Assert.That(dto.DimmerSettings, Is.Null);

        await using var check = factory.CreateDbContext();
        Assert.That(check.RoomDimmerPresets.Count(), Is.Zero);
    }
}
