using Ada.API.Interfaces.Game.Rooms.Pathfinding.ToGo;
using Ada.Game.Rooms.PathFinding.ToGo.Collections.MultiDimensional;

namespace Ada.Game.Rooms.PathFinding.ToGo;

public class WorldGrid : Grid<short>, IWorldGrid
{
    public WorldGrid(short[,] worldArray) : base(worldArray.GetLength(0), worldArray.GetLength(1))
    {
        Buffer.BlockCopy(worldArray, 0, BackingArray, 0, worldArray.Length * sizeof(short));
    }

    public WorldGrid(int height, int width) : base(height, width)
    {
    }
}