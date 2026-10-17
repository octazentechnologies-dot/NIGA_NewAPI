using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;

namespace Homeocentrum.Niga.API.Domain.Helpers
{
    /// <summary>
    /// Matches an admin list row when the query appears in any text or number on that row.
    /// </summary>
    public static class AdminListFieldSearch
    {
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> Properties = new();
        private static readonly HashSet<string> SkipNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "DeleteStatus", "DeletedStatus", "IsDeleted", "IsActive", "Password", "Token",
            "EnteredBy", "ChangedBy"
        };

        public static List<T> Filter<T>(IEnumerable<T> rows, string query, bool ignorePunctuation = false)
        {
            var list = rows?.ToList() ?? new List<T>();
            if (string.IsNullOrWhiteSpace(query)) return list;

            var needle = query.Trim();
            var compactNeedle = ignorePunctuation ? Compact(needle) : null;
            var digits = new string(needle.Where(char.IsDigit).ToArray());
            var digitSearch = digits.Length >= 3;
            return list.Where(row => Matches(row, needle, compactNeedle, digits, digitSearch, 0)).ToList();
        }

        private static bool Matches(object row, string needle, string compactNeedle, string digits, bool digitSearch, int depth)
        {
            if (row == null || depth > 2) return false;

            foreach (var prop in Properties.GetOrAdd(row.GetType(), ReadProperties))
            {
                object value;
                try
                {
                    value = prop.GetValue(row);
                }
                catch
                {
                    continue;
                }

                if (value == null) continue;

                if (value is string text)
                {
                    if (TextMatches(text, needle, compactNeedle, digits, digitSearch)) return true;
                    continue;
                }

                if (value is bool) continue;

                if (value is IEnumerable children && value is not string)
                {
                    foreach (var child in children)
                    {
                        if (child == null) continue;
                        if (child is string childText)
                        {
                            if (TextMatches(childText, needle, compactNeedle, digits, digitSearch)) return true;
                            continue;
                        }

                        if (child is ValueType)
                        {
                            if (TextMatches(child.ToString(), needle, compactNeedle, digits, digitSearch)) return true;
                            continue;
                        }

                        if (Matches(child, needle, compactNeedle, digits, digitSearch, depth + 1)) return true;
                    }
                    continue;
                }

                if (value is ValueType && TextMatches(value.ToString(), needle, compactNeedle, digits, digitSearch))
                    return true;
            }

            return false;
        }

        private static bool TextMatches(string text, string needle, string compactNeedle, string digits, bool digitSearch)
        {
            if (string.IsNullOrEmpty(text)) return false;
            if (text.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (!string.IsNullOrEmpty(compactNeedle) && Compact(text).Contains(compactNeedle)) return true;
            if (!digitSearch) return false;
            var onlyDigits = new string(text.Where(char.IsDigit).ToArray());
            return onlyDigits.Length >= 3 && onlyDigits.Contains(digits);
        }

        /// <summary>Letters and digits only, so "MIND ABDOMINAL" matches "MIND-ABDOMINAL".</summary>
        private static string Compact(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var buffer = new char[value.Length];
            var count = 0;
            foreach (var ch in value)
            {
                if (char.IsLetterOrDigit(ch))
                    buffer[count++] = char.ToLowerInvariant(ch);
            }
            return new string(buffer, 0, count);
        }

        private static PropertyInfo[] ReadProperties(Type type)
        {
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(prop => prop.GetIndexParameters().Length == 0 && prop.CanRead && !SkipNames.Contains(prop.Name))
                .ToArray();
        }
    }
}
