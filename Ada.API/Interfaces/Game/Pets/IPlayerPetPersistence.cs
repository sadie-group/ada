using Ada.API.DTOs.Players;

namespace Ada.API.Interfaces.Game.Pets;

public interface IPlayerPetPersistence
{
    Task SaveRideSettingsAsync(PlayerPetDto pet);

    Task SaveSaddleAsync(PlayerPetDto pet);

    Task SaveBreedingAsync(PlayerPetDto pet);

    Task SaveScratchAsync(PlayerPetDto pet);

    Task DeleteAsync(int petId);
}
