using System.Linq;
using System.Text;

namespace Niga_Domain.Security
{
    /// <summary>PAT-03 / DMO-02 — digits-only compare for IN mobile numbers.</summary>
    public static class PhoneNormalizer
    {
        public static string Digits(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var sb = new StringBuilder(value.Length);
            foreach (var ch in value)
            {
                if (char.IsDigit(ch))
                    sb.Append(ch);
            }

            var digits = sb.ToString();
            if (digits.Length == 12 && digits.StartsWith("91"))
                return digits[2..];
            if (digits.Length == 11 && digits.StartsWith("0"))
                return digits[1..];
            return digits;
        }

        public static bool EqualsNormalized(string? left, string? right)
        {
            var a = Digits(left);
            var b = Digits(right);
            return a.Length >= 8 && a == b;
        }

        public static string Mask(string? value)
        {
            var d = Digits(value);
            if (d.Length <= 4)
                return "****";
            return new string('*', d.Length - 4) + d[^4..];
        }
    }
}
