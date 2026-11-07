using System.Globalization;
using System.Text;

namespace Homeocentrum.Niga.API.Domain.Helpers;

/// <summary>
/// Safe int-id lists for SQL Server <c>IN (...)</c> filters used with EF <c>SqlQuery</c>.
/// <para>
/// Do <b>not</b> pass a CSV string into <c>SqlQuery($"… IN ({{csv}})")</c> —
/// EF parameterizes the whole CSV as one nvarchar, so multi-id households fail with
/// "Conversion failed when converting the nvarchar value '1,2,3' to data type int."
/// </para>
/// Prefer: <c>IN (SELECT CAST([value] AS int) FROM OPENJSON({{ToJsonArray(ids)}}))</c>.
/// </summary>
public static class SqlIntIdList
{
    /// <summary>Positive distinct ids as a JSON array string, e.g. <c>[1,2,3]</c>. Empty → <c>[]</c>.</summary>
    public static string ToJsonArray(IEnumerable<int>? ids)
    {
        if (ids == null)
            return "[]";

        var seen = new HashSet<int>();
        var sb = new StringBuilder("[");
        var first = true;
        foreach (var id in ids)
        {
            if (id <= 0 || !seen.Add(id))
                continue;
            if (!first) sb.Append(',');
            sb.Append(id.ToString(CultureInfo.InvariantCulture));
            first = false;
        }
        sb.Append(']');
        return sb.ToString();
    }

    /// <summary>Positive distinct ids only (stable order not required).</summary>
    public static List<int> Normalize(IEnumerable<int>? ids)
    {
        if (ids == null)
            return new List<int>();
        return ids.Where(id => id > 0).Distinct().ToList();
    }
}
