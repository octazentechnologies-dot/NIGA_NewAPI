using System.Threading;
using System.Threading.Tasks;

namespace Niga_Domain.Services
{
    /// <summary>
    /// SEC-07.02 — SMS provider adapter. Stub until PRE-03 vendor is live.
    /// </summary>
    public interface ISmsSender
    {
        Task<bool> SendAsync(string destination, string message, CancellationToken cancellationToken = default);
    }
}
