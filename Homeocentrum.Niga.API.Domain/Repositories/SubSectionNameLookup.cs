#nullable disable
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.Master;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace Homeocentrum.Niga.API.Domain.Business.Implementation
{
    /// <summary>
    /// Batched name lookups for SectionMaster / SubSectionMaster used to fill display names in list projections.
    /// </summary>
    internal static class SubSectionNameLookup
    {
        private const int BatchSize = 1000;

        public static Dictionary<int, string> GetSectionNames(NIGACentrumContext context)
        {
            return context.SectionMasters
                .AsNoTracking()
                .Select(x => new { x.SectionId, x.SectionName })
                .ToDictionary(x => x.SectionId, x => x.SectionName);
        }

        /// <summary>
        /// Resolves parent subsection names for the given rows. Parents already present in <paramref name="rows"/>
        /// are resolved in memory; the rest are fetched in batches.
        /// </summary>
        public static Dictionary<int, string> GetParentSubSectionNames(NIGACentrumContext context, IEnumerable<SubSectionMaster> rows)
        {
            var list = rows as IList<SubSectionMaster> ?? rows.ToList();
            var known = new Dictionary<int, string>();
            foreach (var row in list)
            {
                known[row.SubSectionId] = row.SubSectionName;
            }

            var result = new Dictionary<int, string>();
            var missing = new HashSet<int>();
            foreach (var row in list)
            {
                if (!row.ParentSubSectionId.HasValue)
                    continue;
                var parentId = row.ParentSubSectionId.Value;
                if (known.TryGetValue(parentId, out var name))
                    result[parentId] = name;
                else
                    missing.Add(parentId);
            }

            foreach (var pair in GetSubSectionNames(context, missing))
            {
                result[pair.Key] = pair.Value;
            }
            return result;
        }

        public static Dictionary<int, string> GetSubSectionNames(NIGACentrumContext context, IEnumerable<int> subSectionIds)
        {
            var result = new Dictionary<int, string>();
            var ids = subSectionIds.Distinct().ToList();
            for (var i = 0; i < ids.Count; i += BatchSize)
            {
                var batch = ids.Skip(i).Take(BatchSize).ToList();
                var rows = context.SubSectionMasters
                    .AsNoTracking()
                    .Where(x => batch.Contains(x.SubSectionId))
                    .Select(x => new { x.SubSectionId, x.SubSectionName })
                    .ToList();
                foreach (var row in rows)
                {
                    result[row.SubSectionId] = row.SubSectionName;
                }
            }
            return result;
        }

        public static string NameOrNull(this Dictionary<int, string> names, int? id)
        {
            return id.HasValue && names.TryGetValue(id.Value, out var name) ? name : null;
        }
    }
}