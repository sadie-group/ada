using Ada.API;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Players.Name;

[PacketId(ServerPacketId.PlayerNameValidation)]
public class PlayerNameValidationWriter : AbstractPacketWriter
{
    public required int ResultCode { get; init; }
    public required string Name { get; init; }
    public required IReadOnlyList<string> Suggestions { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteInteger(ResultCode);
        writer.WriteString(Name);
        writer.WriteInteger(Suggestions.Count);

        foreach (var suggestion in Suggestions)
        {
            writer.WriteString(suggestion);
        }
    }
}
