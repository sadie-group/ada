using System.Collections.Concurrent;
using Ada.API.DTOs.Navigator;
using Ada.API.Interfaces.Game.Navigator;
using Ada.Db;
using Ada.Db.Models.Navigator;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Ada.Game.Navigator;

public class NavigatorTabProvider(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IMapper mapper) : INavigatorTabProvider
{
    private static readonly IReadOnlyList<NavigatorCategoryDto> Empty = [];

    private volatile ConcurrentDictionary<string, IReadOnlyList<NavigatorCategoryDto>> _cache = new();

    public async Task<IReadOnlyList<NavigatorCategoryDto>> GetCategoriesForTabAsync(string? tabName)
    {
        var key = tabName ?? string.Empty;
        var cache = _cache;

        if (cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var tab = await dbContext.Set<NavigatorTab>()
            .AsNoTracking()
            .Include(x => x.Categories)
            .FirstOrDefaultAsync(x => x.Name == tabName);

        var categories = tab == null
            ? Empty
            : mapper.Map<List<NavigatorCategoryDto>>(tab.Categories.OrderBy(x => x.OrderId).ToList());

        cache[key] = categories;

        return categories;
    }

    public void Invalidate() => _cache = new ConcurrentDictionary<string, IReadOnlyList<NavigatorCategoryDto>>();
}
