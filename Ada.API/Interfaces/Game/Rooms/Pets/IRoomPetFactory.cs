using System.Drawing;
using Ada.API.DTOs.Players;

namespace Ada.API.Interfaces.Game.Rooms.Pets;

public interface IRoomPetFactory
{
    IRoomPet Create(
        IRoomLogic room,
        PlayerPetDto pet,
        Point point,
        double pointZ);
}
