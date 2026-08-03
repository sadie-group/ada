using System.Text;
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

        var status = new StringBuilder();

        foreach (var user in Users)
        {
            status.Clear().Append('/');

            var first = true;

            foreach (var entry in user.StatusMap)
            {
                if (!first)
                {
                    status.Append('/');
                }

                first = false;
                status.Append(entry.Key);

                if (!string.IsNullOrEmpty(entry.Value))
                {
                    status.Append(' ').Append(entry.Value);
                }
            }

            while (status.Length > 1 && status[^1] == '/')
            {
                status.Length--;
            }

            writer.WriteLong(user.Player.Player.Id);
            writer.WriteInteger(user.Point.X);
            writer.WriteInteger(user.Point.Y);
            writer.WriteString(user.PointZ.ToString("0.00"));
            writer.WriteInteger((int) user.DirectionHead);
            writer.WriteInteger((int) user.Direction);
            writer.WriteString(status.ToString());
        }
    }
}