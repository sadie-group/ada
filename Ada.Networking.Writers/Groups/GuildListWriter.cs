using Ada.API;
using Ada.API.DTOs.Groups;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Groups;

[PacketId(ServerPacketId.GuildList)]
public class GuildListWriter : AbstractPacketWriter
{
    public required IReadOnlyList<GroupListItemDto> Guilds { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteInteger(Guilds.Count);

        foreach (var guild in Guilds)
        {
            writer.WriteInteger(guild.Id);
            writer.WriteString(guild.Name);
            writer.WriteString(guild.Badge);
            writer.WriteString(guild.ColorA.ToString("X6"));
            writer.WriteString(guild.ColorB.ToString("X6"));
            writer.WriteBool(guild.IsFavourite);
            writer.WriteInteger((int) guild.OwnerId);
            writer.WriteBool(guild.HasForum);
        }
    }
}
