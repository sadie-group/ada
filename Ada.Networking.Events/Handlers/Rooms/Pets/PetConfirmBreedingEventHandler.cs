using System.Text.RegularExpressions;
using Ada.API.Interfaces.Game.Rooms.Pets;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared;
using Ada.Core.Shared.Attributes;
using Ada.Core.Shared.Helpers;
using Ada.Db;
using Ada.Db.Models.Players;
using Ada.Networking.Writers.Players.Inventory;
using Ada.Networking.Writers.Rooms.Pets;
using Ada.Networking.Writers.Rooms.Pets.Breeding;

namespace Ada.Networking.Events.Handlers.Rooms.Pets;

[PacketId(EventHandlerId.PetConfirmBreeding)]
public partial class PetConfirmBreedingEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomRepository roomRepository,
    IMapper mapper) : INetworkPacketEventHandler
{
    public required int NestId { get; init; }
    public required string Name { get; init; }
    public required int PetOneId { get; init; }
    public required int PetTwoId { get; init; }

    public async Task HandleAsync(INetworkClient client)
    {
        if (!RoomContextResolver.TryResolveRoomObjectsForClient(roomRepository, client, out var room, out var roomUser))
        {
            return;
        }

        if (!room.PetRepository.TryGetBreeding(NestId, out var nestPets) ||
            nestPets.PetOneId != PetOneId ||
            nestPets.PetTwoId != PetTwoId)
        {
            return;
        }

        if (!room.PetRepository.TryGetById(PetOneId, out var petOne) || petOne == null ||
            !room.PetRepository.TryGetById(PetTwoId, out var petTwo) || petTwo == null)
        {
            return;
        }

        var playerId = roomUser.Player.Player.Id;

        var eligible = IsBreedableBy(petOne, playerId) && IsBreedableBy(petTwo, playerId);

        if (!eligible)
        {
            return;
        }

        if (Name.Length > PetHelpers.MaximumNameLength || !ValidNameRegex().IsMatch(Name))
        {
            return;
        }

        var averageLevel = (petOne.Pet.Level + petTwo.Pet.Level) / 2.0;
        var offspringLevel = Math.Clamp((int) Math.Round(averageLevel / 2 + GlobalState.Random.Next(0, 3)), 1, PetHelpers.MaximumLevel);

        var offspring = new PlayerPet
        {
            PlayerId = roomUser.Player.Player.Id,
            RoomId = null,
            Name = Name,
            Type = petOne.Pet.Type,
            Race = GlobalState.Random.Next(0, 2) == 0 ? petOne.Pet.Race : petTwo.Pet.Race,
            Color = "ffffff",
            Level = offspringLevel,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        dbContext.PlayerPets.Add(offspring);
        await dbContext.SaveChangesAsync();

        room.PetRepository.StopBreeding(NestId);

        await client.WriteToStreamAsync(new PetPackageNameValidationWriter
        {
            ItemId = NestId,
            ErrorCode = PetPackageNameValidationWriter.CloseWidget,
            ErrorText = "",
        });

        await client.WriteToStreamAsync(new PetBreedingCompletedWriter
        {
            PetId = offspring.Id,
            RarityCategory = 1,
        });

        await client.WriteToStreamAsync(new PlayerInventoryAddPetWriter
        {
            Pet = mapper.Map<PlayerPetDto>(offspring),
        });
    }

    private static bool IsBreedableBy(IRoomPet pet, long playerId)
        => pet.Pet is { GrowthStage: >= 7, IsDead: false } &&
           (pet.Pet.PlayerId == playerId || pet.Pet.PubliclyBreedable);

    [GeneratedRegex("^[a-zA-Z0-9]+$")]
    private static partial Regex ValidNameRegex();
}
