using System.Diagnostics;
using System.Text;
using Ada.API;
using Ada.API.Interfaces.Game.Locale;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Chat.Commands;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Networking.Writers.Players;

namespace Ada.Game.Rooms.Chat.Commands;

public class AboutCommand(
    IRoomRepository roomRepository, 
    IPlayerRepository playerRepository,
    ILocaleService localeService) : AbstractRoomChatCommand
{
    public override string Trigger => "about";
    public override string Description => localeService["cmd.about.describe"];

    public override async Task ExecuteAsync(IRoomUser user, IRoomChatCommandParameterReader reader)
    {
        var assembly = AppDomain.CurrentDomain
            .GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "Ada.Server");

        var version = assembly?.GetName().Version;
        
        var message = new StringBuilder();
        var memoryMb = Process.GetCurrentProcess().PrivateMemorySize64 / (1024 * 1024);

        message.AppendLine($"Ada {version}");
        message.AppendLine("");
        message.AppendLine($"Players Online: {playerRepository.Count()}");
        message.AppendLine($"Rooms Loaded: {roomRepository.Count}");
        message.AppendLine($"Memory Used: {memoryMb} MB");
        message.AppendLine("");
        message.AppendLine("Credits:");
        message.AppendLine("Habtard - Lead Developer");
        message.AppendLine("");
        message.AppendLine("Honorable Mentions");
        message.AppendLine("Damien - Encryption");
        message.AppendLine("");
        
        await user.NetworkObject.WriteToStreamAsync(new PlayerAlertWriter
        {
            Message = message.ToString()
        });
    }
}