using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Players.Favourites;

[PacketId(ServerPacketId.PlayerFavouriteRoomUpdate)]
public class PlayerFavouriteRoomUpdateWriter : AbstractPacketWriter
{
    public required int RoomId { get; init; }
    public required bool Added { get; init; }
}
