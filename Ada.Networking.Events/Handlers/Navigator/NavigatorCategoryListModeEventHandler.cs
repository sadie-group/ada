using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Events.Handlers.Navigator;

[PacketId(EventHandlerId.NavigatorCategoryListMode)]
public class NavigatorCategoryListModeEventHandler : INetworkPacketEventHandler
{
    public string? Category { get; set; }
    public string? Mode { get; set; }

    public Task HandleAsync(INetworkClient client)
    {
        if (client.Player == null || string.IsNullOrWhiteSpace(Category))
        {
            return Task.CompletedTask;
        }

        var mode = Mode?.Trim().ToLowerInvariant() switch
        {
            "thumbnails" => 1,
            _ => 0
        };

        client.Player.State.Navigator.SetListMode(Category.Trim(), mode);

        return Task.CompletedTask;
    }
}
