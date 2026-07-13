using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Players.Other;

[PacketId(ServerPacketId.PlayerPing)]
public class PlayerPingWriter : AbstractPacketWriter;