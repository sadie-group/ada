using Ada.API.DTOs.Catalog.Pages;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Db.Models.Catalog.Pages;
using Ada.Networking.Writers.Catalog;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Catalog;

[PacketId(EventHandlerId.CatalogIndex)]
public class CatalogIndexEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IMapper mapper) : INetworkPacketEventHandler
{
    public async Task HandleAsync(INetworkClient client)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var parentlessPages = await dbContext.Set<CatalogPage>()
            .Include(x => x.Pages.OrderBy(y => y.OrderId))
            .ThenInclude(x => x.Pages.OrderBy(y => y.OrderId))
            .ThenInclude(x => x.Pages.OrderBy(y => y.OrderId))
            .Where(x => x.CatalogPageId == null)
            .ToListAsync();

        await client.WriteToStreamAsync(new CatalogTabsWriter
        {
            Mode = client.Player!.State.CatalogMode,
            TabPages = mapper.Map<List<CatalogPageDto>>(parentlessPages)
        });
    }
}