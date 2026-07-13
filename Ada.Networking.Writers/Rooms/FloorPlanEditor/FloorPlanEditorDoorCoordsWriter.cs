using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.FloorPlanEditor;

[PacketId(ServerPacketId.FloorPlanEditorDoorCoords)]
public class FloorPlanEditorDoorCoordsWriter : AbstractPacketWriter
{
    public required int X { get; init; }
    public required int Y { get; init; }
    public required int Direction { get; init; }
}