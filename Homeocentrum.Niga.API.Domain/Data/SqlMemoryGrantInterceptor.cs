using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Homeocentrum.Niga.API.Domain.Data
{
    /// <summary>
    /// Caps the query memory grant of single SELECT statements.
    /// Many text columns are nvarchar(max), so SQL Server estimates sorts and joins on them at
    /// hundreds of MB. On SQL Server Express (about 1.4 GB of query memory) such a request waits
    /// on RESOURCE_SEMAPHORE until the command times out, and the queries queued behind it fail too.
    /// With the cap the query starts at once and spills to tempdb when it really needs more.
    /// </summary>
    public sealed class SqlMemoryGrantInterceptor : DbCommandInterceptor
    {
        // One shared instance keeps EF's internal service provider cache to a single entry.
        public static readonly SqlMemoryGrantInterceptor Instance = new();

        private const string Hint = " OPTION (MAX_GRANT_PERCENT = 5)";

        private SqlMemoryGrantInterceptor()
        {
        }

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            AddHint(command);
            return result;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            AddHint(command);
            return ValueTask.FromResult(result);
        }

        public override InterceptionResult<object> ScalarExecuting(
            DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
        {
            AddHint(command);
            return result;
        }

        public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<object> result,
            CancellationToken cancellationToken = default)
        {
            AddHint(command);
            return ValueTask.FromResult(result);
        }

        public static void AddHint(DbCommand command)
        {
            if (command.CommandType != System.Data.CommandType.Text)
                return;
            var text = command.CommandText;
            if (string.IsNullOrWhiteSpace(text))
                return;
            if (!text.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                return;
            // Only plain single statements: a hint after a batch, a comment, or an existing OPTION would break the SQL.
            if (text.Contains(';') || text.Contains("--") || text.Contains("/*")
                || text.Contains("OPTION", StringComparison.OrdinalIgnoreCase))
                return;
            command.CommandText = text.TrimEnd() + Hint;
        }
    }
}
