using Ada.API;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Users;

[PacketId(ServerPacketId.RoomUserStatus)]
public class RoomUserStatusWriter : AbstractPacketWriter
{
    public required ICollection<IRoomUser> Users { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteInteger(Users.Count);

        foreach (var user in Users)
        {
            var statusList = user.
                StatusMap.
                Select(x => x.Key + (string.IsNullOrEmpty(x.Value) ? "" : " " + x.Value));
            
            writer.WriteLong(user.Player.Player.Id);
            writer.WriteInteger(user.Point.X);
            writer.WriteInteger(user.Point.Y);
            writer.WriteString(user.PointZ.ToString("0.00"));
            writer.WriteInteger((int) user.DirectionHead);
            writer.WriteInteger((int) user.Direction);
            writer.WriteString("/" + string.Join("/", statusList).TrimEnd('/'));
        }
    }
}