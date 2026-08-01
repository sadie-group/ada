using Ada.API;
using Ada.API.DTOs.Rooms;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Furniture;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Rooms;
using Ada.Networking.Writers.Rooms.Bots;
using Ada.Networking.Writers.Rooms.Furniture;
using Ada.Networking.Writers.Rooms.Pets;
using Ada.Networking.Writers.Rooms.Users;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Game.Rooms.Pets;
using Ada.Db;

namespace Ada.Networking.Events.Handlers.Rooms;

[PacketId(EventHandlerId.RoomHeightmap)]
public class RoomHeightmapEventHandler(IRoomRepository roomRepository,
    IRoomFurnitureItemHelperService roomFurnitureItemHelperService,
    IMapper mapper,
    IPlayerRepository playerRepository,
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomPetFactory roomPetFactory) : INetworkPacketEventHandler
{
    public async Task HandleAsync(INetworkClient client)
    {
        var room = roomRepository.TryGetRoomById(client.Player.State.CurrentRoomId);
        
        if (room == null)
        {
            return;
        }

        var roomTileMap = room.TileMap;
        var userRepository = room.UserRepository;
        var isOwner = room.Room.OwnerId == client.Player.Player.Id;
        
        await client.WriteToStreamAsync(new RoomRelativeMapWriter
        {
            TileMap = roomTileMap
        });
        
        await client.WriteToStreamAsync(new RoomFloorHeightMapWriter
        {
            Scale = true,
            WallHeight = -1,
            RelativeHeightmap = room.Room.Layout.Heightmap.Replace("\r\n", "\r")
        });
        
        await client.WriteToStreamAsync(new RoomWallFloorSettingsWriter
        {
            HideWalls = room.Room.Settings.HideWalls,
            WallThickness = room.Room.Settings.WallThickness,
            FloorThickness = room.Room.Settings.FloorThickness
        });
        
        if (room.BotRepository.Count > 0)
        {
            await client.WriteToStreamAsync(new RoomBotDataWriter
            {
                Bots = room.BotRepository.GetAll()
            });
        
            await client.WriteToStreamAsync(new RoomBotStatusWriter
            {
                Bots = room.BotRepository.GetAll()
            });
        }

        await LoadPetsIfNeededAsync(room);

        if (room.PetRepository.Count > 0)
        {
            await client.WriteToStreamAsync(new RoomPetsWriter
            {
                Pets = room.PetRepository.GetAll()
            });

            await client.WriteToStreamAsync(new RoomPetStatusWriter
            {
                Pets = room.PetRepository.GetAll()
            });
        }

        await SendFurnitureItemsAsync(room.Room, client, playerRepository);

        try
        {
            await client.WriteToStreamAsync(new RoomForwardDataWriter
            {
                Room = room.Room,
                RoomForward = false,
                EnterRoom = true,
                IsOwner = isOwner,
                UsersNow = room.UserRepository.Count,
                OwnerUsername = await playerRepository.GetPlayerUsernameByIdAsync(room.Room.OwnerId) ?? string.Empty
            });
        }
        catch (NullReferenceException)
        {
            var y = 0;
        }
    }

    private async Task SendFurnitureItemsAsync(
        RoomDto room,
        INetworkObject client,
        IPlayerRepository playerRepository)
    {
        var floorItems = room.FurnitureItems
            .Where(x => x.PlayerFurnitureItem.FurnitureItem.Type == FurnitureItemType.Floor)
            .ToList();
        
        var wallItems = room.FurnitureItems
            .Where(x => x.PlayerFurnitureItem.FurnitureItem.Type == FurnitureItemType.Wall)
            .ToList();

        var ownerIds = new HashSet<long>();

        foreach (var item in room.FurnitureItems)
        {
            ownerIds.Add(item.PlayerFurnitureItem.PlayerId);
        }

        var furnitureOwners = new Dictionary<long, string?>(ownerIds.Count);

        foreach (var ownerId in ownerIds)
        {
            furnitureOwners[ownerId] = await playerRepository.GetPlayerUsernameByIdAsync(ownerId) ?? "Unknown User";
        }

        await client.WriteToStreamAsync(new RoomFloorItemsWriter
        {
            FloorItems = floorItems,
            FurnitureOwners = furnitureOwners,
            RoomFurnitureItemHelperService = roomFurnitureItemHelperService
        });

        await client.WriteToStreamAsync(new RoomWallItemsWriter
        {
            FurnitureOwners = furnitureOwners,
            WallItems = wallItems
        });
    }
    private async Task LoadPetsIfNeededAsync(IRoomLogic room)
    {
        if (room.PetRepository.Loaded)
        {
            return;
        }

        room.PetRepository.Loaded = true;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var pets = await dbContext.PlayerPets
            .AsNoTracking()
            .Where(x => x.RoomId == room.Room.Id)
            .Join(dbContext.Players, p => p.PlayerId, o => o.Id, (p, o) => new { Pet = p, OwnerName = o.Username })
            .ToListAsync();

        foreach (var row in pets)
        {
            var petDto = mapper.Map<PlayerPetDto>(row.Pet);
            petDto.OwnerName = row.OwnerName;

            var point = new System.Drawing.Point(row.Pet.X, row.Pet.Y);
            var roomPet = roomPetFactory.Create(room, petDto, point, row.Pet.Z);

            if (room.PetRepository.TryAdd(roomPet))
            {
                room.TileMap.AddUnitToMap(point, roomPet);
            }
        }
    }
}
