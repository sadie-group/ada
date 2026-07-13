namespace Ada.API.Interfaces.Game.Rooms.Pathfinding.ToGo.Collections.MultiDimensional;

public interface IModelAGrid<T>
{
    public int Height { get; }
    public int Width { get; }
    public T this[int row, int column] { get; set; }
    public T this[IPosition position] { get; set; }
    public IEnumerable<IPosition> GetSuccessorPositions(IPosition node, bool optionsUseDiagonals = false);
}