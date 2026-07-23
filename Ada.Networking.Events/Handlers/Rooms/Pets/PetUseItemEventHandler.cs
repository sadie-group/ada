using Microsoft.EntityFrameworkCore;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Core.Shared.Helpers;
using Ada.Db;
using Ada.Networking.Writers.Rooms.Pets;

namespace Ada.Networking.Events.Handlers.Rooms.Pets;

[PacketId(EventHandlerId.PetUseItem)]
public class PetUseItemEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomRepository roomRepository) : INetworkPacketEventHandler
{
    public required int ItemId { get; init; }
    public required int PetId { get; init; }

    public async Task HandleAsync(INetworkClient client)
    {
        if (!RoomContextResolver.TryResolveRoomObjectsForClient(roomRepository, client, out var room, out _))
        {
            return;
        }

        var item = room.Room.FurnitureItems.FirstOrDefault(x => x.Id == ItemId);

        if (item == null)
        {
            return;
        }

        if (!room.PetRepository.TryGetById(PetId, out var roomPet) || roomPet == null)
        {
            return;
        }

        var pet = roomPet.Pet;

        if (pet.Type != PetHelpers.HorseType)
        {
            return;
        }

        var assetName = (item.PlayerFurnitureItem.FurnitureItem.AssetName ?? "").ToLower();

        if (assetName.StartsWith("horse_dye_"))
        {
            var race = int.Parse(assetName.Split('_')[2]);
            var raceType = race switch
            {
                0 => 0,
                >= 13 and <= 17 => (2 + race) * 4 + 1,
                _ => race * 4 - 2,
            };

            pet.Race = raceType;
            await ApplyAsync(x => x.SetProperty(p => p.Race, raceType));
            await BroadcastFigureAsync();
            return;
        }

        if (assetName.StartsWith("horse_hairdye_"))
        {
            var dye = int.Parse(assetName.Split('_')[2]);
            var hairColor = dye switch
            {
                0 => -1,
                1 => 1,
                >= 13 and <= 17 => 68 + dye,
                _ => 48 + dye,
            };

            pet.HairColor = hairColor;
            await ApplyAsync(x => x.SetProperty(p => p.HairColor, hairColor));
            await BroadcastFigureAsync();
            return;
        }

        if (assetName.StartsWith("horse_hairstyle_"))
        {
            var hairStyle = 100 + int.Parse(assetName.Split('_')[2]);

            pet.HairStyle = hairStyle;
            await ApplyAsync(x => x.SetProperty(p => p.HairStyle, hairStyle));
            await BroadcastFigureAsync();
            return;
        }

        if (assetName.StartsWith("horse_saddle"))
        {
            pet.HasSaddle = true;
            await ApplyAsync(x => x.SetProperty(p => p.HasSaddle, true));
            await BroadcastFigureAsync();
        }

        return;

        async Task ApplyAsync(
            System.Linq.Expressions.Expression<Func<Microsoft.EntityFrameworkCore.Query.SetPropertyCalls<Ada.Db.Models.Players.PlayerPet>,
                Microsoft.EntityFrameworkCore.Query.SetPropertyCalls<Ada.Db.Models.Players.PlayerPet>>> setter)
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            await dbContext.PlayerPets.Where(x => x.Id == pet.Id).ExecuteUpdateAsync(setter);
        }

        async Task BroadcastFigureAsync()
        {
            await room.BroadcastDataAsync(new RoomPetHorseFigureWriter { Pet = pet });
        }
    }
}
