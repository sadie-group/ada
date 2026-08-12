using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Networking.Writers.Players.Name;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Players.Name;

[PacketId(EventHandlerId.PlayerCheckValidName)]
public class PlayerCheckValidNameEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory) : INetworkPacketEventHandler, IRunsOutsideRoomLock
{
    public required string Name { get; init; }

    private const int _minLength = 3;
    private const int _maxLength = 15;

    private const int _ok = 0;
    private const int _taken = 4;
    private const int _tooShort = 2;
    private const int _tooLong = 3;
    private const int _invalidCharacters = 5;

    public async Task HandleAsync(INetworkClient client)
    {
        if (client.Player == null)
        {
            return;
        }

        var name = Name.Trim();
        var resultCode = Validate(name);

        if (resultCode == _ok)
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            if (await dbContext.Players.AnyAsync(x => x.Username == name))
            {
                resultCode = _taken;
            }
        }

        await client.WriteToStreamAsync(new PlayerNameValidationWriter
        {
            ResultCode = resultCode,
            Name = name,
            Suggestions = []
        });
    }

    private static int Validate(string name)
    {
        if (name.Length < _minLength)
        {
            return _tooShort;
        }

        if (name.Length > _maxLength)
        {
            return _tooLong;
        }

        return name.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '.' or '_')
            ? _ok
            : _invalidCharacters;
    }
}
