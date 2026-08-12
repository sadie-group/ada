using System.Collections.Concurrent;
using System.Drawing;
using System.Runtime.CompilerServices;
using Ada.API.Collections;
using Ada.API.DTOs.Players.Furniture;
using Ada.Core.Enums.Game.Furniture;
using Ada.Core.Enums.Miscellaneous;

namespace Ada.Game.Rooms.Mapping;

internal static class RoomTileItemIndex
{
    internal sealed class TileItemIndex
    {
        public required int Revision { get; init; }

        public required ConcurrentDictionary<Point, IReadOnlyList<PlayerFurnitureItemPlacementDataDto>> ItemsByTile
        {
            get;
            init;
        }
    }

    private sealed class TileItemIndexSlot
    {
        public volatile TileItemIndex? Current;
    }

    private static readonly ConditionalWeakTable<object, TileItemIndexSlot> ItemIndexCache = new();

    public static void InvalidateItemIndex(IEnumerable<PlayerFurnitureItemPlacementDataDto> items)
    {
        if (ItemIndexCache.TryGetValue(items, out var slot))
        {
            slot.Current = null;
        }
    }

    internal static TileItemIndex GetItemIndex(ICollection<PlayerFurnitureItemPlacementDataDto> items)
    {
        var slot = ItemIndexCache.GetValue(items, static _ => new TileItemIndexSlot());
        var current = slot.Current;

        var (revision, snapshot) = CollectionRevision.SnapshotOf(items);

        if (current != null &&
            revision != CollectionRevision.Untracked &&
            current.Revision == revision)
        {
            return current;
        }

        var rebuilt = BuildItemIndex(snapshot, revision);
        slot.Current = rebuilt;
        return rebuilt;
    }

    public static bool TryMoveItemInIndex(
        ICollection<PlayerFurnitureItemPlacementDataDto> items,
        PlayerFurnitureItemPlacementDataDto item,
        List<Point> oldPoints,
        List<Point> newPoints)
    {
        if (item.PlayerFurnitureItem.FurnitureItem.Type != FurnitureItemType.Floor)
        {
            return true;
        }

        if (!ItemIndexCache.TryGetValue(items, out var slot))
        {
            return false;
        }

        var index = slot.Current;

        if (index == null || !CollectionRevision.IsCurrent(items, index.Revision))
        {
            slot.Current = null;
            return false;
        }

        var itemsByTile = index.ItemsByTile;

        foreach (var point in oldPoints)
        {
            if (!itemsByTile.TryGetValue(point, out var tileItems) || !tileItems.Contains(item))
            {
                continue;
            }

            var replacement = tileItems.Where(x => !ReferenceEquals(x, item)).ToArray();

            if (replacement.Length == 0)
            {
                itemsByTile.TryRemove(point, out _);
            }
            else
            {
                itemsByTile[point] = replacement;
            }
        }

        foreach (var point in newPoints)
        {
            if (!itemsByTile.TryGetValue(point, out var tileItems))
            {
                itemsByTile[point] = new[] { item };
                continue;
            }

            if (tileItems.Contains(item))
            {
                continue;
            }

            itemsByTile[point] = [..tileItems, item];
        }

        return true;
    }

    internal static (int Width, int Length) GetItemFootprint(PlayerFurnitureItemPlacementDataDto item)
    {
        var furnitureItem = item.PlayerFurnitureItem.FurnitureItem;

        return item.Direction switch
        {
            HDirection.East or HDirection.West => (
                furnitureItem.TileSpanY > 0 ? furnitureItem.TileSpanY : 1,
                furnitureItem.TileSpanX > 0 ? furnitureItem.TileSpanX : 1),
            HDirection.North or HDirection.South => (
                furnitureItem.TileSpanX > 0 ? furnitureItem.TileSpanX : 1,
                furnitureItem.TileSpanY > 0 ? furnitureItem.TileSpanY : 1),
            _ => (0, 0)
        };
    }

    private static TileItemIndex BuildItemIndex(
        IReadOnlyList<PlayerFurnitureItemPlacementDataDto> items,
        int revision)
    {
        var itemsByTile = new Dictionary<Point, List<PlayerFurnitureItemPlacementDataDto>>();

        foreach (var item in items)
        {
            if (item.PlayerFurnitureItem.FurnitureItem.Type != FurnitureItemType.Floor)
            {
                continue;
            }

            var (width, length) = GetItemFootprint(item);

            for (var x = item.PositionX; x <= item.PositionX + width - 1; x++)
            {
                for (var y = item.PositionY; y <= item.PositionY + length - 1; y++)
                {
                    var point = new Point(x, y);

                    if (!itemsByTile.TryGetValue(point, out var tileItems))
                    {
                        itemsByTile[point] = tileItems = [];
                    }

                    tileItems.Add(item);
                }
            }
        }

        return new TileItemIndex
        {
            Revision = revision,
            ItemsByTile = new ConcurrentDictionary<Point, IReadOnlyList<PlayerFurnitureItemPlacementDataDto>>(
                itemsByTile.Select(x =>
                    new KeyValuePair<Point, IReadOnlyList<PlayerFurnitureItemPlacementDataDto>>(
                        x.Key,
                        x.Value.ToArray())))
        };
    }

}
