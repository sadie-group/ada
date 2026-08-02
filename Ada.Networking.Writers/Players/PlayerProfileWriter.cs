using Ada.API;
using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Players;

[PacketId(ServerPacketId.PlayerProfile)]
public class PlayerProfileWriter : AbstractPacketWriter
{
    public required PlayerDto Player { get; init; }
    public required bool Online { get; init; }
    public required int FriendshipCount { get; init; }
    public required bool FriendshipExists { get; init; }
    public required bool FriendshipRequestExists { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        var data = Player.Data;
        var avatarData = Player.AvatarData;

        var lastOnline = data?.LastOnline == null
            ? 0
            : (int) (DateTime.Now - data.LastOnline).Value.TotalSeconds;

        writer.WriteLong(Player.Id);
        writer.WriteString(Player.Username);
        writer.WriteString(avatarData?.FigureCode ?? string.Empty);
        writer.WriteString(avatarData?.Motto ?? "");
        writer.WriteString(Player.CreatedAt.ToString("dd MMMM yyyy"));
        writer.WriteLong(data?.AchievementScore ?? 0);
        writer.WriteLong(FriendshipCount);
        writer.WriteBool(FriendshipExists);
        writer.WriteBool(FriendshipRequestExists);
        writer.WriteBool(Online);
        writer.WriteInteger(0);
        writer.WriteInteger(lastOnline);
        writer.WriteBool(true);
    }
}