using System.Threading.Channels;
using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;

namespace Niga_Domain.Services;

public class AudioCaseTakingQueue : IAudioCaseTakingQueue
{
    private readonly Channel<AudioCaseTakingJob> _channel = Channel.CreateUnbounded<AudioCaseTakingJob>(
        new UnboundedChannelOptions { SingleReader = false, SingleWriter = false });
    private int _enqueued;
    private int _dequeued;

    public ChannelReader<AudioCaseTakingJob> Reader => _channel.Reader;

    /// <summary>Approximate jobs still waiting (enqueued − dequeued).</summary>
    public int PendingCount => Math.Max(0, Volatile.Read(ref _enqueued) - Volatile.Read(ref _dequeued));

    public ValueTask EnqueueAsync(AudioCaseTakingJob job, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _enqueued);
        return _channel.Writer.WriteAsync(job, cancellationToken);
    }

    public void MarkDequeued() => Interlocked.Increment(ref _dequeued);
}
