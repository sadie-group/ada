using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.API.Interfaces.Networking;
using Ada.Core.Enums.Game.Players;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Users;

[PacketId(ServerPacketId.RoomUserData)]
public class RoomUserDataWriter : AbstractPacketWriter
{
    public required ICollection<IRoomUser> Users { get; set; }

    public override void OnConfigureRules()
    {
        Override(nameof(Users), writer =>
        {
            Users = Users
                .ToList();
            
            writer.WriteInteger(Users.Count);

            foreach (var user in Users)
            {
                writer.WriteLong(user.Player.Player.Id);
                writer.WriteString(user.Player.Player.Username);
                writer.WriteString(user.Player.Player.AvatarData?.Motto ?? "");
                writer.WriteString(user.Player.Player.AvatarData?.FigureCode ?? "");
                writer.WriteLong(user.Player.Player.Id);
                writer.WriteInteger(user.Point.X);
                writer.WriteInteger(user.Point.Y);
                writer.WriteString(user.PointZ + "");
                writer.WriteInteger((int) user.Direction);
                writer.WriteInteger(1);
                writer.WriteString(user.Player.Player.AvatarData?.Gender == PlayerAvatarGender.Male ? "M" : "F");
                writer.WriteInteger(-1);
                writer.WriteInteger(-1);
                writer.WriteString("");
                writer.WriteString("");
                writer.WriteInteger(user.Player.Player.Data?.AchievementScore ?? 0);
                writer.WriteBool(true);
            }
        });
    }
}