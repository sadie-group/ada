using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Networking;

namespace Ada.API.Interfaces.Game.Rooms.Users;

public interface IRoomUserTrade
{
    List<IRoomUser> Users { get; init; }
    List<PlayerFurnitureItemDto> Items { get; init; }
    Task OfferItemsAsync(List<PlayerFurnitureItemDto> playerItems);
    Task BroadcastToUsersAsync(AbstractPacketWriter writer);
    Task SwapItemsAsync();
    void RemoveOfferedItem(PlayerFurnitureItemDto item);
}