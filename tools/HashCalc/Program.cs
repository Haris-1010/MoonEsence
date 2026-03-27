using System.Security.Cryptography;

const int iterations = 120_000;
const int saltSizeBytes = 16;
const int hashSizeBytes = 32;

var password = args.Length > 0 ? args[0] : "Admin123!";

var saltBytes = RandomNumberGenerator.GetBytes(saltSizeBytes);
var salt = Convert.ToBase64String(saltBytes);

using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, iterations, HashAlgorithmName.SHA256);
var hashBytes = pbkdf2.GetBytes(hashSizeBytes);
var hash = Convert.ToBase64String(hashBytes);

Console.WriteLine($"salt={salt}");
Console.WriteLine($"hash={hash}");
