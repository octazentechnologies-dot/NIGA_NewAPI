using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.Security
{
    /// <summary>
    /// PBKDF2 password hashing with detectable prefix so plaintext legacy values can be migrated lazily.
    /// Format: PBKDF2$v1${iterations}${saltBase64}${hashBase64}
    /// Full hash is ~90+ chars — UserPassword MUST be NVARCHAR(500)+ before writing hashes.
    /// </summary>
    public static class UserPasswordHasher
    {
        public const string Prefix = "PBKDF2$v1$";
        public const int DefaultIterations = 100_000;
        public const int MinWellFormedHashLength = 80;
        private const int SaltSize = 16;
        private const int KeySize = 32;

        public static bool IsHashed(string? stored)
        {
            return !string.IsNullOrEmpty(stored)
                && stored.StartsWith(Prefix, StringComparison.Ordinal);
        }

        public static bool IsCorruptHash(string? stored)
        {
            if (string.IsNullOrEmpty(stored) || !IsHashed(stored))
                return false;
            return !IsWellFormedHash(stored);
        }

        public static bool IsWellFormedHash(string? stored)
        {
            if (string.IsNullOrEmpty(stored) || stored.Length < MinWellFormedHashLength)
                return false;

            var parts = stored.Split('$');
            // Format: PBKDF2$v1${iterations}${saltB64}${hashB64} → exactly 5 segments
            if (parts.Length != 5
                || !string.Equals(parts[0], "PBKDF2", StringComparison.Ordinal)
                || !string.Equals(parts[1], "v1", StringComparison.Ordinal)
                || !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var iterations)
                || iterations < 1)
            {
                return false;
            }

            try
            {
                var salt = Convert.FromBase64String(parts[3]);
                var hash = Convert.FromBase64String(parts[4]);
                return salt.Length == SaltSize && hash.Length == KeySize;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public static string Hash(string password, int iterations = DefaultIterations)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password is required.", nameof(password));

            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = Pbkdf2(password, salt, iterations, KeySize);
            var encoded = string.Join("$",
                "PBKDF2",
                "v1",
                iterations.ToString(CultureInfo.InvariantCulture),
                Convert.ToBase64String(salt),
                Convert.ToBase64String(hash));

            if (encoded.Length < MinWellFormedHashLength)
                throw new InvalidOperationException("Generated password hash is unexpectedly short.");

            return encoded;
        }

        public static bool Verify(string password, string? stored)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(stored))
                return false;

            if (IsCorruptHash(stored))
                return false;

            if (!IsHashed(stored))
                return string.Equals(password, stored, StringComparison.Ordinal);

            if (!IsWellFormedHash(stored))
                return false;

            var parts = stored.Split('$');
            if (!int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var iterations))
                return false;

            byte[] salt;
            byte[] expected;
            try
            {
                salt = Convert.FromBase64String(parts[3]);
                expected = Convert.FromBase64String(parts[4]);
            }
            catch (FormatException)
            {
                return false;
            }

            var actual = Pbkdf2(password, salt, iterations, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }

        private static byte[] Pbkdf2(string password, byte[] salt, int iterations, int keySize)
        {
            return Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                keySize);
        }
    }
}
