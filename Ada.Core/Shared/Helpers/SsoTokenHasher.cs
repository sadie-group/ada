using System.Security.Cryptography;
using System.Text;

namespace Ada.Core.Shared.Helpers;

public static class SsoTokenHasher
{
    public static string Hash(string token)
        => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
