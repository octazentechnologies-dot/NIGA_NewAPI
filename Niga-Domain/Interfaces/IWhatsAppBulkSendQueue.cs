using System;
using System.Threading;
using System.Threading.Tasks;
using Niga_Domain.DTOs;

namespace Niga_Domain.Interfaces;

public interface IWhatsAppBulkSendQueue
{
    ValueTask EnqueueAsync(WhatsAppBulkSendJob job, CancellationToken cancellationToken = default);
}
