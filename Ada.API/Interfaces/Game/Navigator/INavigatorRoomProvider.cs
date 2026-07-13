using Ada.API.DTOs.Rooms;
using Ada.API.Interfaces.Game.Players;

namespace Ada.API.Interfaces.Game.Navigator;

public interface INavigatorRoomProvider
{
    Task<List<RoomDto>> GetRoomsForCategoryNameAsync(IPlayerLogic player, string category);
    Task<List<RoomDto>> GetRoomsForSearchQueryAsync(string searchQuery);
}