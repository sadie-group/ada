using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Players.Navigator;

[PacketId(ServerPacketId.NavigatorSavedSearches)]
public class PlayerSavedSearchesWriter : AbstractPacketWriter
{
    public required ICollection<PlayerSavedSearchDto> Searches { get; init; }
}