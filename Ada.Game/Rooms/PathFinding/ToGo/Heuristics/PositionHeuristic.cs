using System.Runtime.CompilerServices;

namespace Ada.Game.Rooms.PathFinding.ToGo.Heuristics;

internal static class PositionHeuristic
{
    private const int _estimate = 2;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int Calculate(HeuristicFormula formula, in Position source, in Position destination)
    {
        var dRow = Math.Abs(source.Row - destination.Row);
        var dColumn = Math.Abs(source.Column - destination.Column);

        return formula switch
        {
            HeuristicFormula.Manhattan => _estimate * (dRow + dColumn),
            HeuristicFormula.MaxDxdy => _estimate * Math.Max(dRow, dColumn),
            HeuristicFormula.DiagonalShortCut => DiagonalShortCut(dRow, dColumn),
            HeuristicFormula.Euclidean => (int) (_estimate * Math.Sqrt((double) dRow * dRow + (double) dColumn * dColumn)),
            HeuristicFormula.EuclideanNoSqr => _estimate * (dRow * dRow + dColumn * dColumn),
            _ => throw new ArgumentOutOfRangeException(nameof(formula), formula, null)
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int DiagonalShortCut(int dRow, int dColumn)
    {
        var diagonal = Math.Min(dRow, dColumn);
        var straight = dRow + dColumn;

        return _estimate * 2 * diagonal + _estimate * (straight - 2 * diagonal);
    }
}
