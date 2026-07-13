using Ada.API.DTOs.Catalog.Items;
using Ada.API.Interfaces.Networking.Client;

namespace Ada.API.Interfaces.Game.Catalog;

public interface ICatalogPurchaseConfirmationService
{
    Task ConfirmAsync(INetworkClient client, CatalogItemDto item, int amount);
    Task WriteFailureAsync(INetworkClient client);
    Task WriteUnavailableAsync(INetworkClient client);
}