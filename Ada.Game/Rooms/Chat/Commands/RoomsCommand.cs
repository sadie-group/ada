using Ada.API;
using Ada.API.Interfaces.Game.Locale;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Chat.Commands;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Networking.Writers.Players;

namespace Ada.Game.Rooms.Chat.Commands;

public class RoomsCommand(
    IRoomRepository roomRepository, 
    ILocaleService localeService) : AbstractRoomChatCommand
{
    public override string Trigger => "rooms";
    public override string Description => localeService["cmd.rooms.describe"];

    public override async Task ExecuteAsync(IRoomUser user, IRoomChatCommandParameterReader reader)
    {
        await user.NetworkObject.WriteToStreamAsync(new PlayerAlertWriter
        {
            Message = string.Join(
                Environment.NewLine, 
                roomRepository
                    .GetAllRooms()
                    .OrderByDescending(x => x.UserRepository.Count)
                    .Select(r => $"{r.Room.Id}: {r.UserRepository.Count} users, {r.UserRepository.NoUsersSince}"))
        });
    }
}