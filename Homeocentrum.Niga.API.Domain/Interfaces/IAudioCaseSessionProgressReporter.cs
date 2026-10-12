namespace Homeocentrum.Niga.API.Domain.Interfaces;

public interface IAudioCaseSessionProgressReporter
{
    Task ReportAsync(
        Guid sessionId,
        string progressStep,
        int? percentHint = null,
        CancellationToken cancellationToken = default);
}
