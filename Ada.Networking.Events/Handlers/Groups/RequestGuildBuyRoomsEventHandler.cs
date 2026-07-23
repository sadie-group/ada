using Ada.API.Interfaces.Game.Groups;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Groups;

namespace Ada.Networking.Events.Handlers.Groups;

[PacketId(EventHandlerId.RequestGuildBuyRooms)]
public class RequestGuildBuyRoomsEventHandler(IGroupRepository groupRepository) : INetworkPacketEventHandler
{
    public const int GroupCreationPrice = 10;

    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null)
        {
            return;
        }

        var rooms = await groupRepository.GetRoomsForGroupCreationAsync(player.Player.Id);

        await client.WriteToStreamAsync(new GuildBuyRoomsWriter
        {
            PurchasePrice = GroupCreationPrice,
            Rooms = rooms
        });
    }
}
