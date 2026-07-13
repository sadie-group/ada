using System.Drawing;
using Ada.API;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.FloorPlanEditor;

[PacketId(ServerPacketId.FloorPlanEditorOccupiedTiles)]
public class FloorPlanEditorOccupiedTilesWriter : AbstractPacketWriter
{
    public required List<Point> Points { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteInteger(Points.Count);

        foreach (var point in Points)
        {
            writer.WriteInteger(point.X);
            writer.WriteInteger(point.Y);
        }
    }
}