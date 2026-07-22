using Ada.API.DTOs.Furniture;
using Ada.API.DTOs.Players.Furniture;
using Ada.Game.Rooms.Furniture;

namespace Ada.Tests.Game.Rooms.Furniture;

public class FurnitureHeightExtensionsTests
{
    private static PlayerFurnitureItemPlacementDataDto MockItem(
        string? multiHeights,
        string metaData,
        double stackHeight = 1.5) =>
        new()
        {
            PlayerFurnitureItem = new PlayerFurnitureItemDto
            {
                FurnitureItemId = 0,
                FurnitureItem = new FurnitureItemDto
                {
                    InteractionType = "multiheight",
                    Name = "",
                    AssetName = "",
                    StackHeight = stackHeight,
                    MultiHeights = multiHeights
                },
                LimitedData = "",
                MetaData = metaData
            }
        };

    [Test]
    public void GetEffectiveStackHeight_MultiHeightItem_UsesMetaDataIndex()
    {
        Assert.That(MockItem("0;0.5;1", "2").GetEffectiveStackHeight(), Is.EqualTo(1));
    }

    [Test]
    public void GetEffectiveStackHeight_IndexWrapsAroundHeightCount()
    {
        Assert.That(MockItem("0;0.5", "3").GetEffectiveStackHeight(), Is.EqualTo(0.5));
    }

    [Test]
    public void GetEffectiveStackHeight_NoMultiHeights_FallsBackToStackHeight()
    {
        Assert.That(MockItem(null, "").GetEffectiveStackHeight(), Is.EqualTo(1.5));
    }
}
