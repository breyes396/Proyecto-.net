using System.Security.Cryptography;
using System.Text;

namespace AuthServiceIN6BV.Application.Services;

public static class UuidGenerator
{
    private static readonly string Alphabet = "123456789ABCDEFGHJKMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz";
    
    public static string GenerateShorUUID()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[12];
        rng.GetBytes(bytes);
        
    }

}