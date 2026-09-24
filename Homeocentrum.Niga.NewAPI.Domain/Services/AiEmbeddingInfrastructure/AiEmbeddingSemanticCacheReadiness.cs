using Homeocentrum.Niga.NewAPI.Domain.Interfaces;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AiEmbeddingInfrastructure;

/// <summary>
/// Tracks whether enterprise concept + rubric embedding vectors are loaded in memory.
/// Audio rubric discovery must wait for this before running.
/// </summary>
public interface IAiEmbeddingSemanticCacheReadiness
{
    bool IsReady { get; }

    bool IsWarming { get; }

    string? LastError { get; }

    (int ConceptCount, int RubricCount) LoadedCounts { get; }

    void MarkWarming();

    void MarkReady(int conceptCount, int rubricCount);

    void MarkFailed(string error);

    void MarkReadyIfDisabled();

    Task WaitUntilReadyAsync(TimeSpan timeout, CancellationToken cancellationToken = default);
}

public class AiEmbeddingSemanticCacheReadiness : IAiEmbeddingSemanticCacheReadiness
{
    private readonly object _sync = new();
    private TaskCompletionSource _readyTcs = CreateReadyTcs();

    public bool IsReady { get; private set; }

    public bool IsWarming { get; private set; }

    public string? LastError { get; private set; }

    public (int ConceptCount, int RubricCount) LoadedCounts { get; private set; }

    public void MarkWarming()
    {
        lock (_sync)
        {
            IsWarming = true;
            IsReady = false;
            LastError = null;
            if (_readyTcs.Task.IsCompleted)
            {
                _readyTcs = CreateReadyTcs();
            }
        }
    }

    public void MarkReady(int conceptCount, int rubricCount)
    {
        lock (_sync)
        {
            IsReady = true;
            IsWarming = false;
            LastError = null;
            LoadedCounts = (conceptCount, rubricCount);
            _readyTcs.TrySetResult();
        }
    }

    public void MarkFailed(string error)
    {
        lock (_sync)
        {
            IsReady = false;
            IsWarming = false;
            LastError = error;
            _readyTcs.TrySetException(new InvalidOperationException(error));
        }
    }

    public void MarkReadyIfDisabled()
    {
        MarkReady(0, 0);
    }

    public async Task WaitUntilReadyAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        if (IsReady)
        {
            return;
        }

        Task waitTask;
        lock (_sync)
        {
            waitTask = _readyTcs.Task;
        }

        await waitTask.WaitAsync(timeout, cancellationToken);
    }

    private static TaskCompletionSource CreateReadyTcs() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
