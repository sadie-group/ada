using Ada.API;
using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Players.Messenger;

[PacketId(ServerPacketId.PlayerSearchResult)]
public class PlayerSearchResultWriter : AbstractPacketWriter
{
    public required ICollection<PlayerDto> Friends { get; init; }
    public required ICollection<PlayerDto> Strangers { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteInteger(Friends.Count);

        foreach (var friend in Friends)
        {
            writer.WriteLong(friend.Id);
            writer.WriteString(friend.Username);
            writer.WriteString(friend.AvatarData?.Motto ?? string.Empty);
            writer.WriteBool(false);
            writer.WriteBool(false);
            writer.WriteString("");
            writer.WriteInteger(1);
            writer.WriteString(friend.AvatarData?.FigureCode ?? string.Empty);
            writer.WriteString("");
        }
        
        writer.WriteInteger(Strangers.Count);

        foreach (var stranger in Strangers)
        {
            writer.WriteLong(stranger.Id);
            writer.WriteString(stranger.Username);
            writer.WriteString(stranger.AvatarData?.Motto ?? string.Empty);
            writer.WriteBool(false);
            writer.WriteBool(false);
            writer.WriteString("");
            writer.WriteInteger(1);
            writer.WriteString(stranger.AvatarData?.FigureCode ?? string.Empty);
            writer.WriteString("");
        }
    }
}