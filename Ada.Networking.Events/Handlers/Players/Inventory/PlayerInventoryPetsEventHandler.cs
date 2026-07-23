using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Networking.Writers.Players.Inventory;

namespace Ada.Networking.Events.Handlers.Players.Inventory;

[PacketId(EventHandlerId.PetInventory)]
public class PlayerInventoryPetsEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IMapper mapper) : INetworkPacketEventHandler
{
    public async Task HandleAsync(INetworkClient client)
    {
        if (client.Player == null)
        {
            return;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var pets = await dbContext.PlayerPets
            .AsNoTracking()
            .Where(x => x.PlayerId == client.Player.Player.Id && x.RoomId == null)
            .ToListAsync();

        await client.WriteToStreamAsync(new PlayerInventoryPetsWriter
        {
            Pets = pets.Select(mapper.Map<PlayerPetDto>).ToList(),
        });
    }
}
