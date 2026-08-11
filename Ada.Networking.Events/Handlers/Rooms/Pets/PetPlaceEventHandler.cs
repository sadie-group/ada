using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Game.Rooms.Pets;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Game.Rooms;
using Ada.Networking.Writers.Generic;
using Ada.Networking.Writers.Players.Inventory;
using Ada.Networking.Writers.Rooms.Pets;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Drawing;

namespace Ada.Networking.Events.Handlers.Rooms.Pets;

[PacketId(EventHandlerId.PetPlace)]
public class PetPlaceEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomRepository roomRepository,
    IRoomPetFactory roomPetFactory,
    IMapper mapper) : INetworkPacketEventHandler, IDefersPersistence
{
    private const int _maximumPetsPerRoom = 30;

    public required int Id { get; init; }
    public required int X { get; init; }
    public required int Y { get; init; }

    public async Task HandleAsync(INetworkClient client)
    {
        if (!RoomContextResolver.TryResolveRoomObjectsForClient(roomRepository, client, out var room, out var roomUser))
        {
            return;
        }

        var playerId = roomUser.Player.Player.Id;
        var isOwner = room.Room.OwnerId == playerId;

        if (!isOwner && room.Room.Settings?.AllowPets != true)
        {
            await client.WriteToStreamAsync(new PetErrorWriter { ErrorCode = PetErrorWriter.PetsForbiddenInFlat });
            return;
        }

        if (room.PetRepository.Count >= _maximumPetsPerRoom)
        {
            await client.WriteToStreamAsync(new PetErrorWriter { ErrorCode = PetErrorWriter.MaxPets });
            return;
        }

        Db.Models.Players.PlayerPet? pet;

        await using (var readContext = await dbContextFactory.CreateDbContextAsync())
        {
            pet = await readContext.PlayerPets
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == Id && x.PlayerId == playerId && x.RoomId == null);
        }

        if (pet == null)
        {
            return;
        }

        var placePoint = new Point(X, Y);

        if (!room.TileMap.TileExists(placePoint) ||
            room.TileMap.UsersAtPoint(placePoint))
        {
            await client.WriteToStreamAsync(new PetErrorWriter { ErrorCode = PetErrorWriter.SelectedTileNotFree });
            return;
        }

        pet.RoomId = room.Room.Id;
        pet.X = X;
        pet.Y = Y;
        pet.Z = room.TileMap.ZMap[Y, X];

        var petId = pet.Id;
        var placedRoomId = room.Room.Id;
        var placedX = X;
        var placedY = Y;
        var placedZ = pet.Z;

        _persist = async () =>
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            await dbContext.PlayerPets
                .Where(x => x.Id == petId && x.RoomId == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.RoomId, placedRoomId)
                    .SetProperty(x => x.X, placedX)
                    .SetProperty(x => x.Y, placedY)
                    .SetProperty(x => x.Z, placedZ));
        };

        var petDto = mapper.Map<PlayerPetDto>(pet);
        petDto.OwnerName = roomUser.Player.Player.Username ?? "";
        var roomPet = roomPetFactory.Create(room, petDto, placePoint, petDto.Z);

        if (!room.PetRepository.TryAdd(roomPet))
        {
            return;
        }

        room.TileMap.AddUnitToMap(placePoint, roomPet);

        await room.BroadcastDataAsync(new RoomPetsWriter
        {
            Pets = [roomPet],
        });

        await client.WriteToStreamAsync(new PlayerInventoryRemovePetWriter { Id = pet.Id });
    }

    private Func<Task>? _persist;

    public Task PersistAsync() => _persist?.Invoke() ?? Task.CompletedTask;
}
