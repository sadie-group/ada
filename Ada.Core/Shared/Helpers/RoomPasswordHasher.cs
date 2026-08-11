using System.Security.Cryptography;
using System.Text;

namespace Ada.Core.Shared.Helpers;

public static class RoomPasswordHasher
{
    private const string _prefix = "pbkdf2$";
    private const int _saltBytes = 16;
    private const int _hashBytes = 32;
    private const int _iterations = 100_000;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(_saltBytes);
        var hash = Derive(password, salt);

        return _prefix + Convert.ToHexStringLower(salt) + "$" + Convert.ToHexStringLower(hash);
    }

    public static bool Verify(string? stored, string input)
    {
        if (string.IsNullOrEmpty(stored))
        {
            return string.IsNullOrEmpty(input);
        }

        if (stored.StartsWith(_prefix, StringComparison.Ordinal))
        {
            var parts = stored[_prefix.Length..].Split('$');

            if (parts.Length != 2)
            {
                return false;
            }

            byte[] salt;
            byte[] expected;

            try
            {
                salt = Convert.FromHexString(parts[0]);
                expected = Convert.FromHexString(parts[1]);
            }
            catch (FormatException)
            {
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(Derive(input, salt), expected);
        }

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(stored),
            Encoding.UTF8.GetBytes(input));
    }

    private static byte[] Derive(string password, byte[] salt)
        => Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, _iterations, HashAlgorithmName.SHA256, _hashBytes);
}
