using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Rights;

[PacketId(ServerPacketId.RoomRightsRevoked)]
public class RoomRightsRevokedWriter : AbstractPacketWriter;
