using Homeocentrum.Niga.NewAPI.Domain.DTOs;

namespace Homeocentrum.Niga.NewAPI.Domain.Interfaces;

public interface IEnterpriseRubricDiscoveryEngine
{
    Task<EnterpriseRubricDiscoveryResult> DiscoverCandidatesAsync(
        EnterpriseRubricDiscoveryRequest request,
        CancellationToken cancellationToken = default);
}

public interface IPipelineDiagnosticService
{
    Task PersistAsync(
        Guid sessionId,
        PipelineDiagnosticReportModel report,
        CancellationToken cancellationToken = default);

    PipelineStageDiagnosticModel StartStage(string stageName);

    void CompleteStage(PipelineStageDiagnosticModel stage, int outputCount, string? detail = null);

    void FailStage(PipelineStageDiagnosticModel stage, string error, int outputCount = 0);
}
