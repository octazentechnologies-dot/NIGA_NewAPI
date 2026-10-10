using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Master;

namespace Homeocentrum.Niga.API.Domain.Interfaces;

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
