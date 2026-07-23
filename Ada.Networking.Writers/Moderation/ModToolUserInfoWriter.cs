using Ada.API;
using Ada.API.DTOs.Moderation;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Moderation;

[PacketId(ServerPacketId.ModToolsUserInfo)]
public class ModToolUserInfoWriter : AbstractPacketWriter
{
    public required ModToolUserInfoDto User { get; init; }
    public required bool Online { get; init; }
    public required int AccountAgeMinutes { get; init; }
    public required int MinutesSinceLastLogin { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteInteger((int) User.UserId);
        writer.WriteString(User.Username);
        writer.WriteString(User.Look);
        writer.WriteInteger(AccountAgeMinutes);
        writer.WriteInteger(MinutesSinceLastLogin);
        writer.WriteBool(Online);
        writer.WriteInteger(0);
        writer.WriteInteger(0);
        writer.WriteInteger(0);
        writer.WriteInteger(User.BanCount);
        writer.WriteInteger(0);
        writer.WriteString("");
        writer.WriteString("");
        writer.WriteInteger((int) User.UserId);
        writer.WriteInteger(0);
        writer.WriteString(User.Email);
        writer.WriteString($"{User.RankName} ({User.RankId})");
    }
}
