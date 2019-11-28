using System;
using System.Security.Cryptography;

namespace appbox.Security
{
    sealed class PasswordHasher : IPasswordHasher
    {
        //private const int PBKDF2IterCount = 1000;
        //private const int PBKDF2SubkeyLength = 32;
        //private const int SaltSize = 16;

        public byte[] HashPassword(string password)
        {
            if (password == null)
                throw new ArgumentNullException(nameof(password));
            byte[] salt;
            byte[] bytes;
            using (Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(password, 16, 1000))
            {
                salt = rfc2898DeriveBytes.Salt;
                bytes = rfc2898DeriveBytes.GetBytes(32);
            }
            byte[] inArray = new byte[49];
            Buffer.BlockCopy(salt, 0, inArray, 1, 16);
            Buffer.BlockCopy(bytes, 0, inArray, 17, 32);
            return inArray;
        }

        public bool VerifyHashedPassword(byte[] hashedPassword, string password)
        {
            if (hashedPassword == null)
                return false;
            if (password == null)
                throw new ArgumentNullException(nameof(password));

            if (hashedPassword.Length != 49 || (int)hashedPassword[0] != 0)
                return false;
            byte[] salt = new byte[16];
            Buffer.BlockCopy(hashedPassword, 1, salt, 0, 16);
            byte[] a = new byte[32];
            Buffer.BlockCopy(hashedPassword, 17, a, 0, 32);
            byte[] bytes;
            using (Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(password, salt, 1000))
                bytes = rfc2898DeriveBytes.GetBytes(32);

            return a.AsSpan().SequenceEqual(bytes);
        }
    }
}
