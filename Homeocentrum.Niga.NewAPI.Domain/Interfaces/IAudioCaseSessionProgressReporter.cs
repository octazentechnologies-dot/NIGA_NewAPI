namespace Homeocentrum.Niga.NewAPI.Domain.Interfaces;

public interface IAudioCaseSessionProgressReporter
{
    Task ReportAsync(
        Guid sessionId,
        string progressStep,
        int? percentHint = null,
        CancellationToken cancellationToken = default);
}
