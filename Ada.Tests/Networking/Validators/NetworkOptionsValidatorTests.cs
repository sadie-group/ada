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
        var result = _validator.Validate(null, new NetworkOptions { Host = "127.0.0.1", Port = 30000 });
        Assert.That(result.Succeeded, Is.True);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Validate_MissingHost_Fails(string? host)
    {
        var result = _validator.Validate(null, new NetworkOptions { Host = host });
        Assert.That(result.Failed, Is.True);
    }

    [Test]
    public void Validate_CertificateFileDoesNotExist_Fails()
    {
        var options = new NetworkOptions { Host = "127.0.0.1", CertificateFile = "/nonexistent/cert.pfx" };
        var result = _validator.Validate(null, options);
        Assert.That(result.Failed, Is.True);
    }

    [Test]
    public void Validate_ExistingCertificateFile_Succeeds()
    {
        var path = Path.GetTempFileName();
        try
        {
            var options = new NetworkOptions { Host = "127.0.0.1", CertificateFile = path };
            var result = _validator.Validate(null, options);
            Assert.That(result.Succeeded, Is.True);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
