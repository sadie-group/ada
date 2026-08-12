using Ada.API;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Players.Favourites;

[PacketId(ServerPacketId.PlayerFavouriteRooms)]
public class PlayerFavouriteRoomsWriter : AbstractPacketWriter
{
    public required IReadOnlyList<int> RoomIds { get; init; }

    private const int _favouriteRoomLimit = 30;

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteInteger(_favouriteRoomLimit);
        writer.WriteInteger(RoomIds.Count);

        foreach (var roomId in RoomIds)
        {
            writer.WriteInteger(roomId);
        }
    }
}
