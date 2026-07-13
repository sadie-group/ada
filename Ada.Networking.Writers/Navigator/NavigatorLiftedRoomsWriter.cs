using Ada.API;
using Ada.API.DTOs.Rooms;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Navigator;

[PacketId(ServerPacketId.NavigatorLiftedRooms)]
public class NavigatorLiftedRoomsWriter : AbstractPacketWriter
{
    public required List<RoomDto> Rooms { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteInteger(Rooms.Count);

        foreach (var room in Rooms)
        {
            writer.WriteLong(room.Id);
            writer.WriteInteger(0); // unknown
            writer.WriteString(""); // thumbnail?
            writer.WriteString(room.Name);
        }
    }
}