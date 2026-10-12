using System.Threading.Channels;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Interfaces;

namespace Homeocentrum.Niga.API.Domain.Services;

public class WhatsAppBulkSendQueue : IWhatsAppBulkSendQueue
{
    private readonly Channel<WhatsAppBulkSendJob> _channel = Channel.CreateUnbounded<WhatsAppBulkSendJob>(
        new UnboundedChannelOptions { SingleReader = true });

    public ChannelReader<WhatsAppBulkSendJob> Reader => _channel.Reader;

    public ValueTask EnqueueAsync(WhatsAppBulkSendJob job, CancellationToken cancellationToken = default)
    {
        return _channel.Writer.WriteAsync(job, cancellationToken);
    }
}
