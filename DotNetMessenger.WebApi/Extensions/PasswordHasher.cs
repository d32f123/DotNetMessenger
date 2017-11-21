using System;
using System.Linq;
using System.Security.Cryptography;

namespace DotNetMessenger.WebApi.Extensions
{
    public static class PasswordHasher
    {
        public static string PasswordToHash(string password)
        {
            byte[] salt;
            new RNGCryptoServiceProvider().GetBytes(salt = new byte[16]);

            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000);
            var hash = pbkdf2.GetBytes(20);

            var hashBytes = new byte[36];
            Array.Copy(salt, 0, hashBytes, 0, 16);
            Array.Copy(hash, 0, hashBytes, 16, 20);

            return Convert.ToBase64String(hashBytes);
        }

        public static bool ComparePasswordToHash(string password, string storedHash)
        {
            var hashBytes = Convert.FromBase64String(storedHash);
            var hashPass = new byte[20];
            var salt = new byte[16];
            Array.Copy(hashBytes, 0, salt, 0, 16);
            Array.Copy(hashBytes, 16, hashPass, 0, 20);

            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000);
            var hash = pbkdf2.GetBytes(20);

            return hash.SequenceEqual(hashPass);
        }
    }
}