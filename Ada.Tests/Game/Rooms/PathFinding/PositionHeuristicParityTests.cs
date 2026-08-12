using Ada.Game.Rooms.PathFinding.ToGo;
using Ada.Game.Rooms.PathFinding.ToGo.Heuristics;

namespace Ada.Tests.Game.Rooms.PathFinding;

[TestFixture]
public class PositionHeuristicParityTests
{
    private static IEnumerable<HeuristicFormula> Formulas => Enum.GetValues<HeuristicFormula>();

    [TestCaseSource(nameof(Formulas))]
    public void StaticHeuristic_MatchesTheInterfaceImplementation(HeuristicFormula formula)
    {
        var reference = HeuristicFactory.Create(formula);

        for (var row = -30; row <= 30; row += 3)
        {
            for (var column = -30; column <= 30; column += 3)
            {
                var source = new Position(row, column);
                var destination = new Position(column, row);

                Assert.That(
                    PositionHeuristic.Calculate(formula, source, destination),
                    Is.EqualTo(reference.Calculate(source, destination)),
                    $"{formula} disagreed for {source} -> {destination}");
            }
        }
    }

    [Test]
    public void StaticHeuristic_DoesNotAllocate()
    {
        var source = new Position(3, 9);
        var destination = new Position(41, 17);

        PositionHeuristic.Calculate(HeuristicFormula.Manhattan, source, destination);

        var before = GC.GetAllocatedBytesForCurrentThread();

        var total = 0;

        for (var i = 0; i < 10_000; i++)
        {
            total += PositionHeuristic.Calculate(HeuristicFormula.Manhattan, source, destination);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Multiple(() =>
        {
            Assert.That(total, Is.GreaterThan(0));
            Assert.That(allocated, Is.Zero,
                "the heuristic runs once per successor expansion, so it must not box its positions");
        });
    }
}
