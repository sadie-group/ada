using System.Drawing;
using System.Text.RegularExpressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Pets;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Db.Models.Players;
using Ada.Networking.Writers.Rooms.Furniture;
using Ada.Networking.Writers.Rooms.Pets;

namespace Ada.Networking.Events.Handlers.Rooms.Pets;

[PacketId(EventHandlerId.PetPackageName)]
public partial class PetPackageNameEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomRepository roomRepository,
    IRoomPetFactory roomPetFactory,
    IMapper mapper) : INetworkPacketEventHandler
{
    private static readonly Dictionary<string, int> PackagePetTypes = new()
    {
        ["val11_present"] = 11,
        ["gnome_box"] = 26,
        ["leprechaun_box"] = 27,
        ["velociraptor_egg"] = 34,
        ["pterosaur_egg"] = 33,
        ["petbox_epic"] = 32,
    };

    public required int ItemId { get; init; }
    public required string Name { get; init; }

    public async Task HandleAsync(INetworkClient client)
    {
        if (!RoomContextResolver.TryResolveRoomObjectsForClient(roomRepository, client, out var room, out var roomUser))
        {
            return;
        }

        var item = room.Room.FurnitureItems.FirstOrDefault(x => x.PlayerFurnitureItemId == ItemId);

        if (item == null || item.PlayerFurnitureItem.PlayerId != roomUser.Player.Player.Id)
        {
            return;
        }

        if (!ValidNameRegex().IsMatch(Name))
        {
            await client.WriteToStreamAsync(new PetPackageNameValidationWriter
            {
                ItemId = ItemId,
                ErrorCode = PetPackageNameValidationWriter.ContainsInvalidChars,
                ErrorText = "",
            });

            return;
        }

        var assetName = (item.PlayerFurnitureItem.FurnitureItem.AssetName ?? "").ToLower();

        if (!PackagePetTypes.TryGetValue(assetName, out var petType))
        {
            return;
        }

        var point = new Point(item.PositionX, item.PositionY);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var pet = new PlayerPet
        {
            PlayerId = roomUser.Player.Player.Id,
            RoomId = room.Room.Id,
            Name = Name,
            Type = petType,
            Race = 0,
            Color = "ffffff",
            X = point.X,
            Y = point.Y,
            Z = item.PositionZ,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        dbContext.PlayerPets.Add(pet);
        await dbContext.SaveChangesAsync();

        await dbContext.RoomFurnitureItems
            .Where(x => x.Id == item.Id)
            .ExecuteDeleteAsync();

        await dbContext.PlayerFurnitureItems
            .Where(x => x.Id == item.PlayerFurnitureItem.Id)
            .ExecuteDeleteAsync();

        room.Room.FurnitureItems.Remove(item);

        await room.BroadcastDataAsync(new RoomFloorFurnitureItemRemovedWriter
        {
            Id = ItemId.ToString(),
            Expired = false,
            OwnerId = 0,
            Delay = 0,
        });

        var petDto = mapper.Map<PlayerPetDto>(pet);
        petDto.OwnerName = roomUser.Player.Player.Username ?? "";

        var roomPet = roomPetFactory.Create(room, petDto, point, item.PositionZ);

        if (room.PetRepository.TryAdd(roomPet))
        {
            room.TileMap.AddUnitToMap(point, roomPet);

            await room.BroadcastDataAsync(new RoomPetsWriter
            {
                Pets = [roomPet],
            });
        }
    }

    [GeneratedRegex("^[a-zA-Z0-9]+$")]
    private static partial Regex ValidNameRegex();
}
