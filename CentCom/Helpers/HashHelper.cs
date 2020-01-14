using System;
using CentCom.Tupples;

namespace CentCom.Helpers
{
    public class HashHelper
    {
        public static PasswordHashWithSalt CreatePasswordHash(string password)
        {
            var hmac = new System.Security.Cryptography.HMACSHA512();
            byte[] passwordSalt = hmac.Key;
            byte[] passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            
            return PasswordHashWithSalt.Of(passwordHash, passwordSalt);
        }

        public static bool VerifyPasswordHash(string password, PasswordHashWithSalt passwordHashWithSalt)
        {
            if (passwordHashWithSalt.PasswordHash.Length != 64)
            {
                throw new ArgumentException("Invalid length of password hash (64 bytes expected).", nameof(passwordHashWithSalt));
            }

            if (passwordHashWithSalt.PasswordSalt.Length != 128)
            {
                throw new ArgumentException("Invalid length of password salt (128 bytes expected).", nameof(passwordHashWithSalt));
            }

            using (var hmac = new System.Security.Cryptography.HMACSHA512(passwordHashWithSalt.PasswordSalt))
            {
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                for (int i = 0; i < computedHash.Length; i++)
                {
                    if (computedHash[i] != passwordHashWithSalt.PasswordHash[i]) return false;
                }
            }

            return true;
        }
    }
}