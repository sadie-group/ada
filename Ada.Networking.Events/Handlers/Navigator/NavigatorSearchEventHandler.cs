using Ada.API.DTOs.Navigator;
using Ada.API.DTOs.Rooms;
using Ada.API.Interfaces.Game.Navigator;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Db.Models.Navigator;
using Ada.Networking.Writers.Navigator;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Navigator;

[PacketId(EventHandlerId.NavigatorSearch)]
public class NavigatorSearchEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    INavigatorRoomProvider navigatorRoomProvider,
    IRoomRepository roomRepository,
    IPlayerRepository playerRepository,
    IMapper mapper)
    : INetworkPacketEventHandler
{
    public string? TabName { get; set; }
    public string? SearchQuery { get; set; }
    
    public async Task HandleAsync(INetworkClient client)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        
        var tab = await dbContext.Set<NavigatorTab>()
            .Include(x => x.Categories)
            .FirstOrDefaultAsync(x => x.Name == TabName);

        if (tab == null)
        {
            return;
        }

        var dbCategories = tab.
            Categories.
            OrderBy(x => x.OrderId).
            ToList();
        
        var categories = mapper.Map<List<NavigatorCategoryDto>>(dbCategories);

        var categoryRoomMap = new Dictionary<NavigatorCategoryDto, List<RoomDto>>();

        if (!string.IsNullOrEmpty(SearchQuery))
        {
            categoryRoomMap[new NavigatorCategoryDto
            {
                Name = "Search Results",
                CodeName = "",
                OrderId = 0,
                TabId = 1
            }] = await navigatorRoomProvider.GetRoomsForSearchQueryAsync(SearchQuery);
        }
        else
        {
            foreach (var category in categories)
            {
                categoryRoomMap.Add(category, await navigatorRoomProvider.GetRoomsForCategoryNameAsync(client.Player, category.CodeName));
            }
        }
        
        var searchResultPagesWriter = new NavigatorSearchResultPagesWriter
        {
            TabName = TabName,
            SearchQuery = SearchQuery,
            CategoryRoomMap = categoryRoomMap,
            RoomRepository = roomRepository,
            PlayerRepository = playerRepository
        };

        await client.WriteToStreamAsync(searchResultPagesWriter);
    }
}