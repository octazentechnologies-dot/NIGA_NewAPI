using System.Threading;
using System.Threading.Tasks;

namespace Homeocentrum.Niga.NewAPI.Domain.Services
{
    /// <summary>
    /// SEC-07.02 — SMS provider adapter. DI uses <see cref="ConfigurableSmsSender"/> (Stub | Msg91 | Twilio).
    /// </summary>
    public interface ISmsSender
    {
        Task<bool> SendAsync(string destination, string message, CancellationToken cancellationToken = default);
    }
}
