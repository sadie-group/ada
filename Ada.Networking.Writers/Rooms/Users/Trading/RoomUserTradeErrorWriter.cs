using Ada.API.Interfaces.Networking;
using Ada.Core.Enums.Game.Rooms.Users.Trading;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Users.Trading;

[PacketId(ServerPacketId.RoomUserTradeError)]
public class RoomUserTradeErrorWriter : AbstractPacketWriter
{
    public string Username { get; set; } = "";
    public required RoomUserTradeError Code { get; init; }

    public override void OnConfigureRules()
    {
        Override(nameof(Code), 
            writer => writer.WriteInteger((int)Code));
    }
}