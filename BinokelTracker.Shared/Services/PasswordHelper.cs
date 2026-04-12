using System.Security.Cryptography;
using System.Text;

namespace BinokelTracker.Services;

public static class PasswordHelper
{
    public static string Hash(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static bool Verify(string input, string storedHash)
        => Hash(input) == storedHash;
}
