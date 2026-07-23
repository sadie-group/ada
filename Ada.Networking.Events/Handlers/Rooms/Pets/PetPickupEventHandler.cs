using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Networking.Writers.Players.Inventory;
using Ada.Networking.Writers.Rooms.Users;

namespace Ada.Networking.Events.Handlers.Rooms.Pets;

[PacketId(EventHandlerId.PetPickup)]
public class PetPickupEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomRepository roomRepository,
    IMapper mapper) : INetworkPacketEventHandler
{
    public required int Id { get; init; }

    public async Task HandleAsync(INetworkClient client)
    {
        if (!RoomContextResolver.TryResolveRoomObjectsForClient(roomRepository, client, out var room, out var roomUser))
        {
            return;
        }

        if (!room.PetRepository.TryGetById(Id, out var roomPet) || roomPet == null)
        {
            return;
        }

        var playerId = roomUser.Player.Player.Id;

        if (roomPet.Pet.PlayerId != playerId && room.Room.OwnerId != playerId)
        {
            return;
        }

        if (!room.PetRepository.TryRemove(Id, out _))
        {
            return;
        }

        room.TileMap.UnitMap[roomPet.Point].Remove(roomPet);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var pet = await dbContext.PlayerPets.FirstOrDefaultAsync(x => x.Id == Id);

        if (pet == null)
        {
            return;
        }

        pet.RoomId = null;
        await dbContext.SaveChangesAsync();

        await room.BroadcastDataAsync(new RoomUserLeftWriter
        {
            UserId = pet.Id.ToString(),
        });

        await client.WriteToStreamAsync(new PlayerInventoryAddPetWriter
        {
            Pet = mapper.Map<PlayerPetDto>(pet),
        });
    }
}
