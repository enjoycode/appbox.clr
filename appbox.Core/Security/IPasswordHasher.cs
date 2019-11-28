using System;

namespace appbox.Security
{
    public interface IPasswordHasher
    {

        byte[] HashPassword(string password);
        bool VerifyHashedPassword(byte[] hashedPassword, string password);

    }
}
