using Ada.API;
using Ada.API.Interfaces.Networking;
using Ada.Core.Enums.Game.Rooms.Users.Trading;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Users.Trading;

[PacketId(ServerPacketId.RoomUserTradeError)]
public class RoomUserTradeErrorWriter : AbstractPacketWriter
{
    public string Username { get; set; } = "";
    public required RoomUserTradeError Code { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteString(Username ?? "");
        writer.WriteInteger((int) Code);
    }
}