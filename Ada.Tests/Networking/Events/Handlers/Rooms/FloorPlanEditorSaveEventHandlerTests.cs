using Ada.Networking.Events.Handlers.Rooms.FloorPlanEditor;

namespace Ada.Tests.Networking.Events.Handlers.Rooms;

[TestFixture]
public class FloorPlanEditorSaveEventHandlerTests
{
    private const string FourByThree = "0000\r0000\r0000";

    private static List<string> ErrorsFor(int doorX, int doorY, string heightMap = FourByThree) =>
        new FloorPlanEditorSaveEventHandler(null!, null!)
        {
            HeightMap = heightMap,
            DoorX = doorX,
            DoorY = doorY,
            DoorDirection = 2,
            WallSize = 0,
            FloorSize = 0,
            WallHeight = 0
        }.GetErrors();

    private const string OutsideMap = "${notification.floorplan_editor.error.message.entry_tile_outside_map}";
    private const string NotOnTile = "${notification.floorplan_editor.error.message.entry_not_on_tile}";

    [Test]
    public void Door_AtLastValidColumn_IsAccepted()
    {
        Assert.That(ErrorsFor(3, 2), Does.Not.Contain(OutsideMap));
    }

    [TestCase(4, 0, TestName = "Door one column past the right edge")]
    [TestCase(0, 3, TestName = "Door one row past the bottom edge")]
    [TestCase(-1, 0, TestName = "Negative column")]
    [TestCase(0, -1, TestName = "Negative row")]
    public void Door_OutsideTheMap_IsRejected(int doorX, int doorY)
    {
        Assert.That(ErrorsFor(doorX, doorY), Does.Contain(OutsideMap));
    }

    [Test]
    public void Door_OnAHoleInTheFloor_IsRejected()
    {
        Assert.That(ErrorsFor(1, 1, "0000\r0x00\r0000"), Does.Contain(NotOnTile));
    }

    [Test]
    public void Door_OutsideTheMap_DoesNotAlsoIndexForTheHoleCheck()
    {
        Assert.That(ErrorsFor(99, 99), Does.Not.Contain(NotOnTile));
    }
}
