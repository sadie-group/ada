namespace Ada.API.Interfaces.Game.Rooms.Pathfinding.ToGo.Heuristics;

public interface ICalculateHeuristic
{
    int Calculate(IPosition source, IPosition destination);
}