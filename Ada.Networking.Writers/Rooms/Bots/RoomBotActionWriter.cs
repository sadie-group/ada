using Ada.API;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Bots;

[PacketId(ServerPacketId.RoomBotAction)]
public class RoomBotActionWriter : AbstractPacketWriter
{
    public required int BotId { get; init; }
    public required string Name { get; init; }
    public required string Motto { get; init; }
    public required int WalkingMode { get; init; }
    public required IReadOnlyList<string> ChatLines { get; init; }
    public required bool AutoChat { get; init; }
    public required int ChatDelaySeconds { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteInteger(BotId);
        writer.WriteString(Name);
        writer.WriteString(Motto);
        writer.WriteInteger(WalkingMode);
        writer.WriteBool(AutoChat);
        writer.WriteInteger(ChatDelaySeconds);
        writer.WriteInteger(ChatLines.Count);

        foreach (var line in ChatLines)
        {
            writer.WriteString(line);
        }
    }
}
