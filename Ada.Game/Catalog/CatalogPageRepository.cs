using Ada.API.DTOs.Catalog.Pages;
using Ada.API.Interfaces.Game.Catalog;
using Ada.Db;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Ada.Game.Catalog;

public class CatalogPagePageRepository(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IMapper mapper) : ICatalogPageRepository
{
    public IReadOnlyList<CatalogPageDto> Pages { get; private set; } = [];
    
    public async Task LoadAsync()
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var efPages = await context.CatalogPages
            .AsNoTrackingWithIdentityResolution()
            .AsSplitQuery()
            .Include(p => p.Items)
            .ThenInclude(i => i.FurnitureItems)
            .Include(i => i.Pages)
            .ThenInclude(i => i.Pages)
            .ThenInclude(i => i.Pages)
            .ThenInclude(i => i.Pages)
            .ToListAsync();

        Pages = mapper.Map<IReadOnlyList<CatalogPageDto>>(efPages);
    }
}