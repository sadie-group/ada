using Ada.Networking.Options;
using Microsoft.Extensions.Options;

namespace Ada.Networking.Validators;

public class NetworkPacketOptionsValidator : IValidateOptions<NetworkPacketOptions>
{
    public ValidateOptionsResult Validate(string? name, NetworkPacketOptions options)
    {
        return ValidateOptionsResult.Success;
    }
}
