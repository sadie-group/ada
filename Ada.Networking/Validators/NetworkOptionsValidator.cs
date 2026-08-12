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

        if (string.IsNullOrWhiteSpace(options.AllowedOrigins))
        {
            return ValidateOptionsResult.Fail(
                $"{nameof(NetworkOptions)} 'AllowedOrigins' is empty, so every connection would be refused. " +
                "List the origins your client is served from as a comma-separated value, or set it to '*' " +
                "to accept a socket opened from any web page.");
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

        if (!options.UseWss && !options.AllowInsecureTransport)
        {
            return ValidateOptionsResult.Fail(
                $"{nameof(NetworkOptions)} 'UseWss' is false, so logins are refused to keep SSO tokens " +
                "off a plaintext wire, and no client will be able to connect. Enable 'UseWss' with a " +
                "'CertificateFile', or set 'AllowInsecureTransport' to accept the risk on a trusted network.");
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
