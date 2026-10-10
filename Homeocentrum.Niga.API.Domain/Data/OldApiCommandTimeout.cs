using Microsoft.EntityFrameworkCore;

namespace Homeocentrum.Niga.API.Domain.Data
{
    /// <summary>
    /// Old API ran every query with a 90 second command timeout (New default is 30).
    /// Services ported from the Old API raise the scoped context to the same limit.
    /// </summary>
    public static class OldApiCommandTimeout
    {
        public const int Seconds = 90;

        public static void Apply(DbContext context)
        {
            var current = context.Database.GetCommandTimeout();
            if (current == null || current < Seconds)
            {
                context.Database.SetCommandTimeout(Seconds);
            }
        }
    }
}
