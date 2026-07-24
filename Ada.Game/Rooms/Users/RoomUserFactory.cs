using System.Drawing;
using Ada.API;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Core.Enums.Game.Rooms;
using Ada.Core.Enums.Miscellaneous;
using Microsoft.Extensions.DependencyInjection;

namespace Ada.Game.Rooms.Users;

public class RoomUserFactory(IServiceProvider serviceProvider) : IRoomUserFactory
{
    public IRoomUser Create(
        IRoomLogic room,
        INetworkObject networkObject, 
        Point point, 
        double pointZ,
        HDirection directionHead,
        HDirection direction, 
        IPlayerLogic player,
        RoomControllerLevel controllerLevel)
    {
        return ActivatorUtilities.CreateInstance<RoomUser>(
            serviceProvider,
            room,
            networkObject, 
            point, 
            pointZ,
            directionHead, 
            direction, 
            player,
            controllerLevel);
    }
}