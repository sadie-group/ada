using Ada.Networking.Options;
using Ada.Networking.Validators;

namespace Ada.Tests.Networking.Validators;

[TestFixture]
public class NetworkOptionsValidatorTests
{
    private readonly NetworkOptionsValidator _validator = new();

    [Test]
    public void Validate_ValidOptions_Succeeds()
    {
        var result = _validator.Validate(null, new NetworkOptions { Host = "127.0.0.1", Port = 30000, AllowInsecureTransport = true });
        Assert.That(result.Succeeded, Is.True);
    }

    [Test]
    public void Validate_DefaultConnectionCaps_Succeed()
    {
        var options = new NetworkOptions { Host = "127.0.0.1", Port = 30000, AllowInsecureTransport = true };

        Assert.Multiple(() =>
        {
            Assert.That(options.MaxConnections, Is.GreaterThan(0), "the cap must be on by default");
            Assert.That(options.MaxConnectionsPerAddress, Is.GreaterThan(0));
            Assert.That(_validator.Validate(null, options).Succeeded, Is.True);
        });
    }

    [Test]
    public void Validate_NegativeMaxConnections_Fails()
    {
        var options = new NetworkOptions { Host = "127.0.0.1", MaxConnections = -1, AllowInsecureTransport = true };

        Assert.That(_validator.Validate(null, options).Failed, Is.True);
    }

    [Test]
    public void Validate_NegativeMaxConnectionsPerAddress_Fails()
    {
        var options = new NetworkOptions { Host = "127.0.0.1", MaxConnectionsPerAddress = -1, AllowInsecureTransport = true };

        Assert.That(_validator.Validate(null, options).Failed, Is.True);
    }

    [Test]
    public void Validate_PerAddressCapAboveGlobalCap_Fails()
    {
        var options = new NetworkOptions
        {
            Host = "127.0.0.1",
            AllowInsecureTransport = true,
            MaxConnections = 10,
            MaxConnectionsPerAddress = 50
        };

        Assert.That(_validator.Validate(null, options).Failed, Is.True,
            "a per-address cap that can never be reached is a misconfiguration worth reporting");
    }

    [Test]
    public void Validate_CapsExplicitlyDisabled_Succeeds()
    {
        var options = new NetworkOptions
        {
            Host = "127.0.0.1",
            AllowInsecureTransport = true,
            MaxConnections = 0,
            MaxConnectionsPerAddress = 0
        };

        Assert.That(_validator.Validate(null, options).Succeeded, Is.True);
    }

    [Test]
    public void Validate_PlaintextTransportWithoutOptIn_Fails()
    {
        var options = new NetworkOptions
        {
            Host = "127.0.0.1",
            UseWss = false,
            AllowInsecureTransport = false
        };

        Assert.That(_validator.Validate(null, options).Failed, Is.True);
    }

    [Test]
    public void Validate_PlaintextTransportWithExplicitOptIn_Succeeds()
    {
        var options = new NetworkOptions
        {
            Host = "127.0.0.1",
            UseWss = false,
            AllowInsecureTransport = true
        };

        Assert.That(_validator.Validate(null, options).Succeeded, Is.True);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Validate_MissingHost_Fails(string? host)
    {
        var result = _validator.Validate(null, new NetworkOptions { Host = host, AllowInsecureTransport = true });
        Assert.That(result.Failed, Is.True);
    }

    [Test]
    public void Validate_CertificateFileDoesNotExist_Fails()
    {
        var options = new NetworkOptions { Host = "127.0.0.1", AllowInsecureTransport = true, CertificateFile = "/nonexistent/cert.pfx" };
        var result = _validator.Validate(null, options);
        Assert.That(result.Failed, Is.True);
    }

    [Test]
    public void Validate_ExistingCertificateFile_Succeeds()
    {
        var path = Path.GetTempFileName();
        try
        {
            var options = new NetworkOptions { Host = "127.0.0.1", UseWss = true, CertificateFile = path };
            var result = _validator.Validate(null, options);
            Assert.That(result.Succeeded, Is.True);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
