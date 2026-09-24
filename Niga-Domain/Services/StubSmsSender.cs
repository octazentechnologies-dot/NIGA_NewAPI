using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Niga_Domain.Security;

namespace Niga_Domain.Services
{
    /// <summary>
    /// SEC-07.02 — Does not call a carrier. Logs a masked destination only.
    /// Prefer <see cref="ConfigurableSmsSender"/> (DI default). Kept for explicit stub tests.
    /// </summary>
    public sealed class StubSmsSender : ISmsSender
    {
        private readonly ILogger<StubSmsSender> _logger;

        public StubSmsSender(ILogger<StubSmsSender> logger)
        {
            _logger = logger;
        }

        public Task<bool> SendAsync(string destination, string message, CancellationToken cancellationToken = default)
        {
            var masked = PhoneNormalizer.Mask(destination);
            _logger.LogInformation(
                "SMS stub (PRE-03 vendor not configured). DestinationMasked={Masked} Length={Length}",
                masked,
                (message ?? string.Empty).Length);
            return Task.FromResult(true);
        }
    }
}
