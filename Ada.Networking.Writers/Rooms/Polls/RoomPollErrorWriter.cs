using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Polls;

[PacketId(ServerPacketId.RoomPollError)]
public class RoomPollErrorWriter : AbstractPacketWriter;