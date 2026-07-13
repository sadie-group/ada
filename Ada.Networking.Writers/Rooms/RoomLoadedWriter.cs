using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms;

[PacketId(ServerPacketId.RoomLoaded)]
public class RoomLoadedWriter : AbstractPacketWriter;