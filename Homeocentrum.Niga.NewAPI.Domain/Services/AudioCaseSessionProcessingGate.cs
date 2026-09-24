using System.Collections.Concurrent;

namespace Homeocentrum.Niga.NewAPI.Domain.Services;

/// <summary>
/// Phase 35: prevent duplicate concurrent processing of the same audio case session.
/// </summary>
public interface IAudioCaseSessionProcessingGate
{
    bool TryEnter(Guid sessionId);

    void Exit(Guid sessionId);

    bool IsInFlight(Guid sessionId);
}

public sealed class AudioCaseSessionProcessingGate : IAudioCaseSessionProcessingGate
{
    private readonly ConcurrentDictionary<Guid, byte> _inflight = new();

    public bool TryEnter(Guid sessionId) => _inflight.TryAdd(sessionId, 0);

    public void Exit(Guid sessionId) => _inflight.TryRemove(sessionId, out _);

    public bool IsInFlight(Guid sessionId) => _inflight.ContainsKey(sessionId);
}
