using Ada.API.DTOs.Catalog.FrontPage;
using Ada.API.Interfaces.Game.Catalog;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Catalog;

namespace Ada.Networking.Events.Handlers.Catalog;

[PacketId(EventHandlerId.CatalogPage)]
public class CatalogPageEventHandler(
    List<CatalogFrontPageItemDto> catalogFrontPageItems,
    ICatalogPageRepository pageRepository) : INetworkPacketEventHandler
{
    public int PageId { get; set; }
    public int OfferId { get; set; }
    public string? CatalogMode { get; set; }
    
    public async Task HandleAsync(INetworkClient client)
    {
        if (client.Player == null)
        {
            return;
        }

        var page = pageRepository
            .Pages
            .FirstOrDefault(x => x.Id == PageId);

        if (page == null || !CatalogPageAccess.CanAccess(page, client.Player))
        {
            return;
        }

        await client.WriteToStreamAsync(new CatalogPageWriter
        {
            PageId = page.Id,
            PageLayout = page.Layout,
            Images = page.ImagesJson?.Cast<string?>().ToList() ?? [],
            Texts = page.TextsJson?.Cast<string?>().ToList() ?? [],
            Items = page.Items.ToList(),
            CatalogMode = CatalogMode,
            AcceptSeasonCurrencyAsCredits = false,
            FrontPageItems = catalogFrontPageItems,
            Unknown = -1
        });
    }
}