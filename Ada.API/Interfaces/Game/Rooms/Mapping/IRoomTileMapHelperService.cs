using System.Drawing;
using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Core.Enums.Game.Rooms.Mapping;
using Ada.Core.Enums.Game.Rooms.Users;
using Ada.Core.Enums.Miscellaneous;

namespace Ada.API.Interfaces.Game.Rooms.Mapping;

public interface IRoomTileMapHelperService
{
    HDirection GetOppositeDirection(HDirection direction);

    List<Point> GetPointsForPlacement(
        int x, 
        int y, 
        int width, 
        int length, 
        HDirection direction);

    RoomTileState GetTileState(
        int x, 
        int y, 
        IEnumerable<PlayerFurnitureItemPlacementDataDto> furnitureItems);

    List<PlayerFurnitureItemPlacementDataDto> GetItemsForPosition(int x,
        int y,
        IEnumerable<PlayerFurnitureItemPlacementDataDto> items);

    void InvalidateItemIndex(IEnumerable<PlayerFurnitureItemPlacementDataDto> items);

    short[,] GetWorldArrayFromTileMap(IRoomTileMap map,
        Point goalPoint,
        List<Point> overridePoints);

    void UpdateTileMapsForPoints(
        List<Point> points, 
        IRoomTileMap tileMap, 
        ICollection<PlayerFurnitureItemPlacementDataDto> furnitureItems);

    bool CanPlaceAt(
        IEnumerable<Point> points,
        IRoomTileMap tileMap,
        bool checkForUsers = true);

    bool CanPlaceAt(
        IEnumerable<Point> points,  
        IRoomTileMap tileMap,
        ICollection<PlayerFurnitureItemPlacementDataDto> furnitureItems,
        bool checkForUsers = true);

    List<IRoomUser> GetUsersAtPoints(IEnumerable<Point> points, IEnumerable<IRoomUser> users);
    Point GetPointInFront(int x, int y, HDirection direction, int offset = 0);

    double GetItemPlacementHeight(
        IRoomTileMap roomTileMap,
        IEnumerable<Point> pointsForPlacement, 
        ICollection<PlayerFurnitureItemPlacementDataDto> roomFurnitureItems);

    int GetSquaresBetweenPoints(Point a, Point b);
    RoomUserEffect GetEffectFromInteractionType(string interactionType);
}