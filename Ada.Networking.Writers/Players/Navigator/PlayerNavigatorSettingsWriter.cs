using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Players.Navigator;

[PacketId(ServerPacketId.NavigatorSettings)]
public class PlayerNavigatorSettingsWriter : AbstractPacketWriter
{
    public required PlayerNavigatorSettingsDto NavigatorSettings { get; init; }
}