using Ada.API.DTOs.Server;
using Ada.Core.Enums.Game.WordFilter;

namespace Ada.API.Interfaces.Game.WordFilter;

public interface IWordFilterService
{
    WordFilterResultDto Filter(string text, WordFilterContext context);
    Task ReloadAsync();
}
