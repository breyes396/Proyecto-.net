using AuthServiceIN6BV.Application.Interfaces;
using Konscious.Security.Cryptography;
using System.Data.SqlTypes;
using System.Security.Cryptography;
using System.Text;
namespace AuthServiceIN6BV.Application.Services;
public class PasswordHashService : IPasswordHashService
{
    private const int SaltSize = 16; //bytes que generara para la contraseña
    private const int HashSize = 32; //la longitud 256 bytes
    private const int Iterations = 2; //cuantas veces le pasaremos el Hash a la contraseña
    private const int Memory = 102400; // tamaño que usara (100 mb)
    private const int Parallelism = 8; 
    public string HashPassword(string password)
    {
        //Encriptacion de la contraseña
        var salt = new byte[SaltSize];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = Parallelism,
            Iterations = Iterations,
            MemorySize = Memory
        };

        var hash = argon2.GetBytes(HashSize);

        //casteop para que sea compatible con node.js
        var saltBase64 = Convert.ToBase64String(salt);
        var hashBase64 = Convert.ToBase64String(hash);

        //formato argon2 para node.js
        return $"$argon2id$v=19$m={Memory},t={Iterations},p={Parallelism}${saltBase64}${hashBase64}";

    }
    public bool VerifyPassword(string password, string hashedPassword)
    {
        try
        {
            Console.WriteLine($"[DEBUG Verifying password for hash: {hashedPassword.Substring(0, Math.Min(50, hashedPassword.Length))}...]");
            if (hashedPassword.StartsWith("$"))
            {
                Console.WriteLine("[DEBUG] Using Argon2 standard format verification");
                var result = VerifyArgon2StandardFormat(password, hashedPassword);
                Console.WriteLine($"[DEBUG] Verification result: {result}");
                return result;
            }
            else
            {
                Console.WriteLine("[DEBUG] Using legacy format verification");
                return VerifyLegacyFormat(password, hashedPassword);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Exception in VerifyPassword: {ex.Message}");
            return false;
        }



    }
    private bool VerifyArgon2StandardFormat(string password, string hashedPassword)
    {
        try
        {
            //convertimos la contraseña que se ingreso en bytes
            var argon2Verifier = new Argon2id(Encoding.UTF8.GetBytes(password));
            var parts = hashedPassword.Split('$');
            if(parts.Length !=6) return false;
            
            //extraemos la contraseña hasheada de argon2
            var paramsPart = parts[3];
            var saltBase64 = parts[4];
            var hashBase64 = parts[5];

            var parameters = paramsPart.Split(',');
            var memory = int.Parse(parameters[0].Split('=')[1]);
            var iterations = int.Parse(parameters[1].Split('=')[1]);
            var parallelism = int.Parse(parameters[2].Split('=')[1]);

            var salt = Convert.FromBase64String(FromBase64UrlSafe(saltBase64));
            var expectedHash = Convert.FromBase64String(FromBase64UrlSafe(hashBase64));

            argon2Verifier.Salt = salt;
            argon2Verifier.DegreeOfParallelism = parallelism;
            argon2Verifier.Iterations = iterations;
            argon2Verifier.MemorySize = memory;

            var computedHash = argon2Verifier.GetBytes(expectedHash.Length);

            //igualar y verificar valores de hash sin exponer contraseña, argon2 no la sabe
            return expectedHash.SequenceEqual(computedHash);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error verifying Argon2 standard format: {ex.Message}");
            return false;
        }
    }

    private bool VerifyLegacyFormat(string password, string hashedPassword)
    {
        //contraseña a bytes
        var hashBytes = Convert.FromBase64String(hashedPassword);
        var salt = new byte[SaltSize];
        var hash = new byte[HashSize];
        
        //separa de dos formas independientes poara poder verifcar la contraseña
        Array.Copy(hashBytes, 0, salt, 0, SaltSize);
        Array.Copy(hashBytes, SaltSize, hash, 0, HashSize);

        var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = Parallelism,
            Iterations = Iterations,
            MemorySize = Memory
        };

        //si la contraseña coincide con la que esta ingresando el usuario y la contraseña hashead dejara acceder
        var computedHash = argon2.GetBytes(HashSize);
        return hash.SequenceEqual(computedHash);
        
    }

    //formato legible para .net
    private static string FromBase64UrlSafe(string base64UrlSafe)
    {
        string base64 = base64UrlSafe.Replace('-', '+').Replace('_', '/');
        switch(base64.Length % 4)
        {
            case 2:
                base64 += "==";
                break;
            case 3:
                base64 += "=";
                break;

        }

        return base64;
        
    }
}



