using System.Collections.Concurrent;
using System.Drawing;
using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms.Unit;

namespace Ada.API.Interfaces.Game.Rooms.Mapping;

public interface IRoomTileMap
{
    int SizeX { get; }
    int SizeY { get; }
    int Size { get; }
    short[,] Map { get; }
    ConcurrentDictionary<Point, List<IRoomUnitData>> UnitMap { get; }
    short[,] EffectMap { get; }
    short[,] ZMap { get; set; }
    short[,] TileExistenceMap { get; set; }
    void UpdateEffectMapForTile(int x, int y, ICollection<PlayerFurnitureItemPlacementDataDto> furnitureItems, PlayerFurnitureItemPlacementDataDto? excludeItem = null);
    void AddUnitToMap(Point point, IRoomUnitData unit);
    bool UsersAtPoint(Point point);
    bool TileExists(Point point);
    Point? FirstExistingTile();
}
