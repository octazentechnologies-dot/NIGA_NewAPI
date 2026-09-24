using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Merging;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Enterprise.Quality;

/// <summary>Phase 5: separate authoritative repertory rubrics from AI clinical concepts.</summary>
public static class EnterpriseRubricPresentationHelper
{
    public const string ResultKindRepertory = "RepertoryRubric";
    public const string ResultKindAiConcept = "AiClinicalConcept";

    public static AudioCaseSuggestedRubricModel MarkRepertoryRubric(
        AudioCaseSuggestedRubricModel rubric,
        string? repertoryPath = null,
        string? selectionReason = null)
    {
        rubric.ResultKind = ResultKindRepertory;
        rubric.IsAiSuggested = false;
        rubric.MatchSource ??= RubricDiscoverySources.RepertoryDb;
        rubric.RepertoryPath = repertoryPath;
        rubric.SelectionReason = selectionReason ?? rubric.WhySuggested;
        rubric.RequiresManualApproval = true;
        return rubric;
    }

    public static AudioCaseSuggestedRubricModel CreateAiClinicalConcept(
        HomeopathicConceptNodeModel homeo,
        ClinicalConceptNodeModel? clinical,
        PatientMeaningNodeModel? meaning,
        string engineVersion)
    {
        var conceptLabel = homeo.ConceptName.Trim();
        return new AudioCaseSuggestedRubricModel
        {
            SubSectionId = 0,
            SubSectionName = conceptLabel,
            ResultKind = ResultKindAiConcept,
            IsAiSuggested = true,
            MatchSource = "AiClinicalConcept",
            MatchLayer = "AiClinicalConcept",
            RubricTier = "AiConcept",
            ValidationStatus = "AiConceptOnly",
            RequiresManualApproval = true,
            RequiresDoctorReview = true,
            EngineVersion = engineVersion,
            SourceConceptId = homeo.HomeopathicConceptId is > 0
                ? ConceptIdentity.FromHomeopathicConceptId(homeo.HomeopathicConceptId.Value)
                : null,
            MatchedFrom = meaning?.RawStatement ?? clinical?.ConceptName,
            WhySuggested = "AI clinical concept — no matching repertory rubric found in database.",
            SelectionReason = "Not found in repertory. Display under AI Clinical Concepts; do not repertorize as rubric.",
            ConfidenceScore = homeo.Confidence,
            MatchScore = homeo.Confidence,
            EvidenceChain = new RubricEvidenceChainModel
            {
                PatientStatements = new List<string> { meaning?.NormalizedMeaning ?? meaning?.RawStatement ?? string.Empty },
                ClinicalMeanings = new List<string> { clinical?.ConceptName ?? string.Empty, homeo.ConceptName },
                TranscriptExcerpt = meaning?.RawStatement,
                EvidenceStrength = homeo.Confidence,
            },
        };
    }

    public static bool IsAuthoritativeRepertory(AudioCaseSuggestedRubricModel rubric) =>
        rubric.SubSectionId > 0
        && !string.Equals(rubric.ResultKind, ResultKindAiConcept, StringComparison.OrdinalIgnoreCase);

    public static List<AudioCaseSuggestedRubricModel> PartitionResults(
        List<AudioCaseSuggestedRubricModel> validatedRepertory,
        List<AudioCaseSuggestedRubricModel> aiConcepts) =>
        validatedRepertory
            .Select(r => MarkRepertoryRubric(r, r.RepertoryPath, r.SelectionReason))
            .Concat(aiConcepts)
            .ToList();
}
