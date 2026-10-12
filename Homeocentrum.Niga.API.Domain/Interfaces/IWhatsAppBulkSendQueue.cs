using System;
using System.Threading;
using System.Threading.Tasks;
using Homeocentrum.Niga.API.Domain.DTOs;

namespace Homeocentrum.Niga.API.Domain.Interfaces;

public interface IWhatsAppBulkSendQueue
{
    ValueTask EnqueueAsync(WhatsAppBulkSendJob job, CancellationToken cancellationToken = default);
}
