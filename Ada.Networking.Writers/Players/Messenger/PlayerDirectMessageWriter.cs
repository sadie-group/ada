using Ada.API;
using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;
using Ada.Core.Shared.Extensions;

namespace Ada.Networking.Writers.Players.Messenger;

[PacketId(ServerPacketId.PlayerMessage)]
public class PlayerDirectMessageWriter : AbstractPacketWriter
{
    public required PlayerMessageDto Message { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteLong(Message.OriginPlayerId);
        writer.WriteString(Message.Message ?? "");
        writer.WriteLong(DateTime.Now.ToUnix() - Message.CreatedAt.ToUnixTimeSeconds());
    }
}