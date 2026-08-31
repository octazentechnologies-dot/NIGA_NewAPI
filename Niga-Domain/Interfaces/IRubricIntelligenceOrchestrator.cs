using Niga_Domain.DTOs;
using Niga_Domain.Master;

namespace Niga_Domain.Interfaces;

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
