using System.Security.Cryptography;
using System.Text.RegularExpressions;
using WaterDelivery.Application.Interfaces;

namespace WaterDelivery.Infrastructure.Services
{
    public class PasswordService : IPasswordService
    {
        private const int SaltSize = 32;
        private const int HashSize = 64;
        private const int Iterations = 100000;

        public string GenerateSalt()
        {
            var saltBytes = new byte[SaltSize];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(saltBytes);
            return Convert.ToBase64String(saltBytes);
        }

        public string HashPassword(string password, string salt)
        {
            var saltBytes = Convert.FromBase64String(salt);
            using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Iterations, HashAlgorithmName.SHA256);
            var hashBytes = pbkdf2.GetBytes(HashSize);
            return Convert.ToBase64String(hashBytes);
        }

        public bool VerifyPassword(string password, string hash, string salt)
        {
            var computedHash = HashPassword(password, salt);
            return hash == computedHash;
        }

        public bool IsValidPassword(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8)
                return false;

            var hasUpper = Regex.IsMatch(password, @"[A-Z]");
            var hasLower = Regex.IsMatch(password, @"[a-z]");
            var hasNumber = Regex.IsMatch(password, @"\d");
            var hasSymbol = Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]");

            return hasUpper && hasLower && hasNumber && hasSymbol;
        }
    }
}