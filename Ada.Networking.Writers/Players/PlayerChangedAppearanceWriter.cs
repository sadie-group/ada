using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Players;

[PacketId(ServerPacketId.PlayerChangedAppearance)]
public class PlayerChangedAppearanceWriter : AbstractPacketWriter
{
    public required string FigureCode { get; init; }
    public required string Gender { get; init; }
}