using Ada.API.Interfaces.Game.Rooms.Pathfinding.ToGo;
using Ada.Game.Rooms.PathFinding.ToGo.Collections.MultiDimensional;

namespace Ada.Game.Rooms.PathFinding.ToGo;

public class WorldGrid : Grid<short>, IWorldGrid
{
    public WorldGrid(short[,] worldArray) : base(worldArray.GetLength(0), worldArray.GetLength(1))
    {
        for (var row = 0; row < worldArray.GetLength(0); row++)
        {
            for (var column = 0; column < worldArray.GetLength(1); column++)
            {
                this[row, column] = worldArray[row, column];
            }
        }
    }
}