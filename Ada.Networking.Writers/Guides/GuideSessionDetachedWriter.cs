using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Guides;

[PacketId(ServerPacketId.GuideSessionDetached)]
public class GuideSessionDetachedWriter : AbstractPacketWriter;
