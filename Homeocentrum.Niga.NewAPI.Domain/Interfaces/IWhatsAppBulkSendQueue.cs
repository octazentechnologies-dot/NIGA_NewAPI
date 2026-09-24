using System;
using System.Threading;
using System.Threading.Tasks;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;

namespace Homeocentrum.Niga.NewAPI.Domain.Interfaces;

public interface IWhatsAppBulkSendQueue
{
    ValueTask EnqueueAsync(WhatsAppBulkSendJob job, CancellationToken cancellationToken = default);
}
