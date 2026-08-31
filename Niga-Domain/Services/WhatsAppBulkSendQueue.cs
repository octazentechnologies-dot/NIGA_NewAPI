using System.Threading.Channels;
using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;

namespace Niga_Domain.Services;

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
