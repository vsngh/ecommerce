using System.Security.Cryptography;
using System.Text;

namespace Users.Api.Security;

public static class PasswordHasher
{
    private const string Prefix = "sha256:";

    public static string Hash(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return $"{Prefix}{Convert.ToHexString(bytes).ToLowerInvariant()}";
    }

    public static bool Verify(string password, string storedPasswordHash)
    {
        if (storedPasswordHash.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(Hash(password), storedPasswordHash, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(password, storedPasswordHash, StringComparison.Ordinal);
    }
}
