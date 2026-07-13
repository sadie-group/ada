using System.Drawing;
using Microsoft.Extensions.DependencyInjection;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Bots;

namespace Ada.Game.Rooms.Bots;

public class RoomBotFactory(IServiceProvider serviceProvider) : IRoomBotFactory
{
    public IRoomBot Create(
        IRoomLogic room,
        int id, 
        Point point,
        double pointZ)
    {
        return ActivatorUtilities.CreateInstance<RoomBot>(
            serviceProvider,
            id,
            room,
            point,
            pointZ);
    }
}