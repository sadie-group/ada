using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Users.Trading;

[PacketId(ServerPacketId.RoomUserTradeCompleted)]
public class RoomUserTradeCompletedWriter : AbstractPacketWriter;