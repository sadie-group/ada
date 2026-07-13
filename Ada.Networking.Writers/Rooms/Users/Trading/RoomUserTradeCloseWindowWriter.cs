using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Users.Trading;

[PacketId(ServerPacketId.RoomUserTradeCloseWindow)]
public class RoomUserTradeCloseWindowWriter : AbstractPacketWriter;