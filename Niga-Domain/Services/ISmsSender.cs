using System.Threading;
using System.Threading.Tasks;

namespace Niga_Domain.Services
{
    /// <summary>
    /// SEC-07.02 — SMS provider adapter.
    /// TODO PRE-03: implement MSG91 / Twilio / chosen vendor + DLT templates; keep this interface.
    /// </summary>
    public interface ISmsSender
    {
        Task<bool> SendAsync(string destination, string message, CancellationToken cancellationToken = default);
    }
}
