using Microsoft.EntityFrameworkCore;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Core.Shared.Helpers;
using Ada.Db;
using Ada.Networking.Writers.Rooms.Pets;

namespace Ada.Networking.Events.Handlers.Rooms.Pets;

[PacketId(EventHandlerId.PetRideSettings)]
public class PetRideSettingsEventHandler(
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

        if (pet.PlayerId != roomUser.Player.Player.Id || pet.Type != PetHelpers.HorseType)
        {
            return;
        }

        pet.AnyoneCanRide = !pet.AnyoneCanRide;

        if (!pet.AnyoneCanRide && roomPet.RiderId != null && roomPet.RiderId != pet.PlayerId)
        {
            roomPet.RiderId = null;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        await dbContext.PlayerPets
            .Where(x => x.Id == pet.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.AnyoneCanRide, pet.AnyoneCanRide));

        await client.WriteToStreamAsync(new RoomPetHorseFigureWriter
        {
            Pet = pet,
        });
    }
}
