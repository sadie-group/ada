using Ada.API.Interfaces.Game.Players.Friendships;
using Ada.API;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Players.Messenger;

[PacketId(ServerPacketId.PlayerFriendRequests)]
public class PlayerFriendRequestsWriter : AbstractPacketWriter
{
    public required int TotalRequests { get; init; }
    public required List<IPlayerFriendshipRequestData> Requests { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteInteger(TotalRequests);
        writer.WriteInteger(Requests.Count);

        foreach (var request in Requests)
        {
            writer.WriteLong(request.Id);
            writer.WriteString(request.Username ?? "");
            writer.WriteString(request.FigureCode ?? "");
        }
    }
}
