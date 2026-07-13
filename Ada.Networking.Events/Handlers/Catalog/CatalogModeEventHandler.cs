using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Catalog;

namespace Ada.Networking.Events.Handlers.Catalog;

[PacketId(EventHandlerId.CatalogMode)]
public class CatalogModeEventHandler : INetworkPacketEventHandler
{
    public string? Mode { get; set; }
    
    public async Task HandleAsync(INetworkClient client)
    {
        if (string.IsNullOrWhiteSpace(Mode))
        {
            return;
        }
        
        client.Player!.State.CatalogMode = Mode;
        
        await client.WriteToStreamAsync(new CatalogModeWriter
        {
            Mode = Mode == "BUILDERS_CLUB" ? 1 : 0
        });
    }
}