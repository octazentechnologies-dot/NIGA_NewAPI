using System;

namespace Homeocentrum.Niga.NewAPI.Domain.Helpers
{
    public static class ReceptionStaffPasswordHelper
    {
        public static bool VerifyPassword(string plainPassword, string storedPassword)
        {
            var encodedPassword = CommonMethods.Encoding(plainPassword);
            if (string.Equals(storedPassword, encodedPassword, StringComparison.Ordinal))
            {
                return true;
            }

            try
            {
                return string.Equals(CommonMethods.Decoding(storedPassword), plainPassword, StringComparison.Ordinal);
            }
            catch
            {
                return string.Equals(storedPassword, plainPassword, StringComparison.Ordinal);
            }
        }
    }
}
