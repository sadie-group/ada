using Ada.API;
using Ada.API.Interfaces.Game.Locale;
using Ada.API.Interfaces.Game.Rooms.Chat.Commands;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.API.Interfaces.Game.WordFilter;
using Ada.Networking.Writers.Players;

namespace Ada.Game.Rooms.Chat.Commands;

public class ReloadWordFilterCommand(
    IWordFilterService wordFilterService,
    ILocaleService localeService) : AbstractRoomChatCommand
{
    public override string Trigger => "reloadwordfilter";
    public override string Description => localeService["cmd.reloadwordfilter.describe"];
    public override List<string> PermissionsRequired { get; set; } = ["word_filter_reload"];

    public override async Task ExecuteAsync(IRoomUser user, IRoomChatCommandParameterReader reader)
    {
        await wordFilterService.ReloadAsync();

        await user.NetworkObject.WriteToStreamAsync(new PlayerAlertWriter
        {
            Message = "Word filter reloaded."
        });
    }
}
