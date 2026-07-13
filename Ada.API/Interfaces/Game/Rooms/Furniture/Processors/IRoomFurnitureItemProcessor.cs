using Ada.API.Interfaces.Networking;

namespace Ada.API.Interfaces.Game.Rooms.Furniture.Processors;

public interface IRoomFurnitureItemProcessor
{
    Task<IEnumerable<AbstractPacketWriter>> GetUpdatesForRoomAsync(IRoomLogic roomLogic);
}