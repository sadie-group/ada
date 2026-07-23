using Ada.API;
using Ada.API.DTOs.Groups;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Groups;

[PacketId(ServerPacketId.GuildBuyRooms)]
public class GuildBuyRoomsWriter : AbstractPacketWriter
{
    private static readonly int[][] BadgePartConfig =
    [
        [10, 3, 4], [25, 17, 5], [25, 17, 3], [29, 11, 4], [0, 0, 0]
    ];

    public required int PurchasePrice { get; init; }
    public required IReadOnlyList<GroupCreationRoomDto> Rooms { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteInteger(PurchasePrice);

        writer.WriteInteger(Rooms.Count);

        foreach (var room in Rooms)
        {
            writer.WriteInteger(room.Id);
            writer.WriteString(room.Name);
            writer.WriteBool(false);
        }

        writer.WriteInteger(BadgePartConfig.Length);

        foreach (var triplet in BadgePartConfig)
        {
            writer.WriteInteger(triplet[0]);
            writer.WriteInteger(triplet[1]);
            writer.WriteInteger(triplet[2]);
        }
    }
}
