using Ada.Networking.Options;
using Microsoft.Extensions.Options;

namespace Ada.Networking.Validators;

public class NetworkOptionsValidator : IValidateOptions<NetworkOptions>
{
    public ValidateOptionsResult Validate(string? name, NetworkOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Host))
        {
            return ValidateOptionsResult.Fail($"{nameof(NetworkOptions)} 'Host' cannot be null or empty.");
        }

        if (!string.IsNullOrEmpty(options.CertificateFile) && !File.Exists(options.CertificateFile))
        {
            return ValidateOptionsResult.Fail($"{nameof(NetworkOptions)} 'CertificateFile' is set but doesn't exist.");
        }

        if (options.MaxConnections < 0)
        {
            return ValidateOptionsResult.Fail(
                $"{nameof(NetworkOptions)} 'MaxConnections' cannot be negative; use 0 to disable the limit.");
        }

        if (options.MaxConnectionsPerAddress < 0)
        {
            return ValidateOptionsResult.Fail(
                $"{nameof(NetworkOptions)} 'MaxConnectionsPerAddress' cannot be negative; use 0 to disable the limit.");
        }

        if (options.MaxConnections > 0 &&
            options.MaxConnectionsPerAddress > options.MaxConnections)
        {
            return ValidateOptionsResult.Fail(
                $"{nameof(NetworkOptions)} 'MaxConnectionsPerAddress' ({options.MaxConnectionsPerAddress}) exceeds " +
                $"'MaxConnections' ({options.MaxConnections}), so the per-address limit can never be reached.");
        }

        return ValidateOptionsResult.Success;
    }
}
