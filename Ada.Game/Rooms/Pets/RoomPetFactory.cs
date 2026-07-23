using System.Drawing;
using Microsoft.Extensions.DependencyInjection;
using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Pets;

namespace Ada.Game.Rooms.Pets;

public class RoomPetFactory(IServiceProvider serviceProvider) : IRoomPetFactory
{
    public IRoomPet Create(
        IRoomLogic room,
        PlayerPetDto pet,
        Point point,
        double pointZ)
    {
        return ActivatorUtilities.CreateInstance<RoomPet>(
            serviceProvider,
            room,
            point,
            pointZ,
            pet);
    }
}
