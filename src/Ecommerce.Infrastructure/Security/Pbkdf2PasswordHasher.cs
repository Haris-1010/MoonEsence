using System.Security.Cryptography;
using Ecommerce.Application.Abstractions.Auth;

namespace Ecommerce.Infrastructure.Security;

public class Pbkdf2PasswordHasher : IPasswordHasher
{
    // NOTE: Parameters are production-grade defaults; tune iterations based on your performance needs.
    private const int Iterations = 120_000;
    private const int SaltSizeBytes = 16;
    private const int HashSizeBytes = 32;
    
    public string HashPassword(string password, out string salt)
    {
        salt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(SaltSizeBytes));

        var saltBytes = Convert.FromBase64String(salt);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Iterations, HashAlgorithmName.SHA256);
        var hashBytes = pbkdf2.GetBytes(HashSizeBytes);
        return Convert.ToBase64String(hashBytes);
    }

    public bool VerifyPassword(string password, string salt, string hash)
    {
        var saltBytes = Convert.FromBase64String(salt);
        var expectedHashBytes = Convert.FromBase64String(hash);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Iterations, HashAlgorithmName.SHA256);
        var actualHashBytes = pbkdf2.GetBytes(HashSizeBytes);

        return CryptographicOperations.FixedTimeEquals(expectedHashBytes, actualHashBytes);
    }
}

