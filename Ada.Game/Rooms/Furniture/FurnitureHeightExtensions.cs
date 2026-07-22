using System.Globalization;
using Ada.API.DTOs.Furniture;
using Ada.API.DTOs.Players.Furniture;

namespace Ada.Game.Rooms.Furniture;

public static class FurnitureHeightExtensions
{
    /// <summary>
    /// The stack height a placed item currently presents. Multi-height items
    /// (e.g. adjustable tables) store "0;0.5;1" style heights on the furniture
    /// and pick one via the placed item's metadata index.
    /// </summary>
    public static double GetEffectiveStackHeight(this PlayerFurnitureItemPlacementDataDto item)
    {
        var furniture = item.PlayerFurnitureItem.FurnitureItem;
        var heights = furniture.GetMultiHeights();

        if (heights.Count == 0)
        {
            return furniture.StackHeight;
        }

        var index = int.TryParse(item.PlayerFurnitureItem.MetaData, out var parsed) ? parsed : 0;

        return heights[Math.Abs(index) % heights.Count];
    }

    public static List<double> GetMultiHeights(this FurnitureItemDto furniture)
    {
        if (string.IsNullOrEmpty(furniture.MultiHeights))
        {
            return [];
        }

        return furniture.MultiHeights
            .Split(';')
            .Select(x => double.TryParse(x, NumberStyles.Float, CultureInfo.InvariantCulture, out var height)
                ? height
                : 0)
            .ToList();
    }
}
