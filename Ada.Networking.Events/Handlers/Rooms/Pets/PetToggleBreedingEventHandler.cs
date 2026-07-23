using Microsoft.EntityFrameworkCore;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Core.Shared.Helpers;
using Ada.Db;
using Ada.Networking.Writers.Rooms.Pets;

namespace Ada.Networking.Events.Handlers.Rooms.Pets;

[PacketId(EventHandlerId.PetToggleBreeding)]
public class PetToggleBreedingEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomRepository roomRepository) : INetworkPacketEventHandler
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

        var pet = roomPet.Pet;

        if (pet.PlayerId != roomUser.Player.Player.Id || pet.Type != PetHelpers.MonsterPlantType)
        {
            return;
        }

        pet.PubliclyBreedable = !pet.PubliclyBreedable;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        await dbContext.PlayerPets
            .Where(x => x.Id == pet.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.PubliclyBreedable, pet.PubliclyBreedable));

        await room.BroadcastDataAsync(new PetStatusUpdateWriter
        {
            RoomUnitId = pet.Id,
            AnyoneCanRide = pet.AnyoneCanRide ? 1 : 0,
            CanBreed = pet.GrowthStage >= 7 && !pet.IsDead,
            NotFullyGrown = pet.GrowthStage < 7,
            IsDead = pet.IsDead,
            PubliclyBreedable = pet.PubliclyBreedable,
        });
    }
}
