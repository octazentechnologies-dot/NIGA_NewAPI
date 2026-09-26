using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Master;

namespace Homeocentrum.Niga.NewAPI.Domain.Interfaces;

public interface IRubricIntelligenceOrchestrator
{
    Task<RubricIntelligenceAnalysisResult> AnalyzeAsync(
        AudioCaseSession session,
        string transcript,
        List<AudioCaseSymptomModel> symptoms,
        AudioCaseSummaryModel? summary,
        string correlationId,
        PatientClinicalContext? patientContext = null,
        CancellationToken cancellationToken = default);
}
