using Ada.API;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Filter;

[PacketId(ServerPacketId.RoomFilterList)]
public class RoomFilterListWriter : AbstractPacketWriter
{
    public required IReadOnlyList<string> Words { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteInteger(Words.Count);

        foreach (var word in Words)
        {
            writer.WriteString(word);
        }
    }
}
