using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Homeocentrum.Niga.NewAPI.Domain.Security;

namespace Homeocentrum.Niga.NewAPI.Domain.Helpers
{
    /// <summary>
    /// Reception staff passwords use the same PBKDF2 format as UserMaster (<see cref="UserPasswordHasher"/>).
    /// Rows written before that were URL-encoded base64 of the password (reversible), or plaintext; they still verify
    /// so they can be re-hashed on the next login or by the startup migration.
    /// </summary>
    public static class ReceptionStaffPasswordHelper
    {
        public static string HashPassword(string plainPassword) => UserPasswordHasher.Hash(plainPassword);

        public static bool NeedsRehash(string? storedPassword) => !UserPasswordHasher.IsHashed(storedPassword);

        public static bool VerifyPassword(string plainPassword, string? storedPassword)
        {
            if (string.IsNullOrEmpty(plainPassword) || string.IsNullOrEmpty(storedPassword))
                return false;

            if (UserPasswordHasher.IsHashed(storedPassword))
                return UserPasswordHasher.Verify(plainPassword, storedPassword);

            if (FixedTimeEquals(storedPassword, CommonMethods.Encoding(plainPassword)))
                return true;
            return FixedTimeEquals(LegacyPlaintext(storedPassword), plainPassword);
        }

        /// <summary>Recovers the password from a legacy encoded value; values that do not decode to printable ASCII are treated as plaintext.</summary>
        public static string LegacyPlaintext(string storedPassword)
        {
            try
            {
                var bytes = Convert.FromBase64String(System.Web.HttpUtility.UrlDecode(storedPassword));
                if (bytes.Length > 0 && bytes.All(b => b >= 0x20 && b < 0x7F))
                    return Encoding.ASCII.GetString(bytes);
            }
            catch (FormatException)
            {
            }
            return storedPassword;
        }

        private static bool FixedTimeEquals(string a, string b)
            => CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
    }
}
