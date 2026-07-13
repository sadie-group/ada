using System.Drawing;

namespace Ada.API.Interfaces.Game.Rooms.Pathfinding.ToGo;

public interface IWorldGrid
{
    int Height { get; }
    int Width { get; }
    IEnumerable<IPosition> GetSuccessorPositions(IPosition node, bool optionsUseDiagonals = false);
    short this[Point point] { get; set; }
    short this[IPosition position] { get; set; }
    short this[int row, int column] { get; set; }
}