using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Guides;

[PacketId(ServerPacketId.GuideSessionPartnerIsTyping)]
public class GuideSessionPartnerIsTypingWriter : AbstractPacketWriter
{
    public required bool Typing { get; init; }
}
