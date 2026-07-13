using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Handshake;

[PacketId(ServerPacketId.SecureLogin)]
public class SecureLoginWriter : AbstractPacketWriter;