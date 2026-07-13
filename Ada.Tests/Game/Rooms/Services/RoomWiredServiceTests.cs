using AutoMapper;
using Moq;
using Ada.API.DTOs.Players.Furniture;
using Ada.Core.Enums.Game.Furniture;
using Ada.Game.Rooms.Furniture;
using Ada.Game.Rooms.Services;
using Ada.Tests.Common;

namespace Ada.Tests.Game.Rooms.Services;

public class RoomWiredServiceTests : MockHelpers
{
    [Test]
    public void GetEffectsForTrigger_TriggersInStack_ReturnsJustEffects()
    {
        var dbFactory = TestDbFactory.CreateDbFactory();
        var playerRepository = CreatePlayerRepositoryMock();
        var mapper = new Mock<IMapper>();
        var furnitureItemHelperService = new RoomFurnitureItemHelperService(dbFactory, playerRepository.Object, mapper.Object);
        var wiredService = new RoomWiredService(dbFactory, furnitureItemHelperService);
        var trigger = MockFurnitureItemPlacementData(FurnitureItemInteractionType.WiredTriggerEnterRoom);
        
        var items = new List<PlayerFurnitureItemPlacementDataDto>
        {
            trigger,
            MockFurnitureItemPlacementData(FurnitureItemInteractionType.WiredEffectShowMessage, 0, 0, 1),
            MockFurnitureItemPlacementData(FurnitureItemInteractionType.WiredEffectKickUser, 0, 0, 2),
            MockFurnitureItemPlacementData(FurnitureItemInteractionType.WiredEffectShowMessage, 0, 0, 4),
            MockFurnitureItemPlacementData(FurnitureItemInteractionType.WiredTriggerEnterRoom, 0, 0, 3),
            MockFurnitureItemPlacementData(FurnitureItemInteractionType.WiredEffectShowMessage, 0, 0, 4)
        };

        var effects = wiredService.GetEffectsForTrigger(trigger, items);
        
        Assert.That(effects.Count, Is.EqualTo(2));
    }
}