using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms;

[PacketId(ServerPacketId.RoomEnterError)]
public class RoomEnterErrorWriter : AbstractPacketWriter
{
    public required int ErrorCode { get; init; }

    public override void OnConfigureRules()
    {
        After(GetType().GetProperty(nameof(ErrorCode))!, writer =>
        {
            writer.WriteString("");
        });
    }
}