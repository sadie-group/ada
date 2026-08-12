using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Game.Rooms.Unit;

namespace Ada.API.Interfaces.Game.Rooms.Pets;

public interface IRoomPet : IRoomUnit
{
    PlayerPetDto Pet { get; }
    long? RiderId { get; set; }
}
