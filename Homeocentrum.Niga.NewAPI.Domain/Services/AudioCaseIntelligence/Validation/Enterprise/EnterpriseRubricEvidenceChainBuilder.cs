using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.V3.Engines;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Validation.Enterprise;

public class EnterpriseRubricEvidenceChainBuilder : IRubricEnterpriseEvidenceChainBuilder
{
    public RubricEnterpriseEvidenceChainModel Build(
        AudioCaseSuggestedRubricModel rubric,
        RubricEvidenceChainEnrichmentContext context)
    {
        var graph = context.Graph;
        var discovery = ResolveDiscovery(rubric, graph);
        var candidate = ResolveCandidate(rubric, context);
        var validation = rubric.EnterpriseValidation
            ?? context.ValidationReports.FirstOrDefault(r => r.SubSectionId == rubric.SubSectionId);

        var (homeo, clinical, meaning) = ResolveGraphNodes(rubric, graph, discovery, candidate, context.Concepts);
        context.DoctorFeedbackBySubSectionId.TryGetValue(rubric.SubSectionId, out var feedback);

        var legacyChain = rubric.EvidenceChain;
        var v3Chain = candidate?.EvidenceChain;

        var transcript = FirstNonEmpty(
            meaning?.RawStatement,
            v3Chain?.TranscriptStatement,
            legacyChain?.TranscriptExcerpt,
            rubric.MatchedFrom,
            ExtractTranscriptExcerpt(context.Transcript, rubric.MatchedFrom));

        var meaningText = FirstNonEmpty(
            meaning?.NormalizedMeaning,
            v3Chain?.PatientMeaning,
            legacyChain?.PatientStatements.FirstOrDefault(),
            rubric.MatchedFrom);

        var clinicalText = FirstNonEmpty(
            clinical?.ConceptName,
            v3Chain?.ClinicalConcept,
            legacyChain?.ClinicalMeanings.FirstOrDefault(),
            context.Concepts.FirstOrDefault(c =>
                rubric.MatchedFrom != null
                && (c.RawStatement.Contains(rubric.MatchedFrom, StringComparison.OrdinalIgnoreCase)
                    || (c.ClinicalMeaning?.Contains(rubric.MatchedFrom, StringComparison.OrdinalIgnoreCase) ?? false)))?.ClinicalMeaning);

        var homeopathicText = FirstNonEmpty(
            homeo?.ConceptName,
            candidate?.SourceConceptName,
            v3Chain?.HomeopathicConcept,
            legacyChain?.ClinicalMeanings.Skip(1).FirstOrDefault(),
            context.Concepts.FirstOrDefault(c =>
                string.Equals(c.HomeopathicMeaning, rubric.MatchedFrom, StringComparison.OrdinalIgnoreCase))?.HomeopathicMeaning);

        var embeddingValue = candidate != null
            ? $"{candidate.MappedFromConceptKey ?? candidate.SourceConceptName} → {candidate.SubSectionName} ({candidate.MatchMethod})"
            : rubric.MatchLayer ?? rubric.MatchSource ?? "ConceptMapping";

        var confidenceValue = rubric.ConfidenceScore ?? rubric.MatchScore;
        var validationValue = validation != null
            ? validation.PassedAllSteps ? "Accepted" : "Rejected"
            : rubric.ValidationStatus ?? "Pending";

        var steps = new List<RubricEvidenceChainStepModel>
        {
            BuildStep(RubricEvidenceChainStepKeys.Transcript, "Transcript", transcript),
            BuildStep(RubricEvidenceChainStepKeys.Meaning, "Meaning", meaningText, meaning?.Confidence),
            BuildStep(RubricEvidenceChainStepKeys.ClinicalConcept, "Clinical Concept", clinicalText, clinical?.Confidence),
            BuildStep(RubricEvidenceChainStepKeys.HomeopathicConcept, "Homeopathic Concept", homeopathicText, homeo?.Confidence, new Dictionary<string, string>
            {
                ["Category"] = homeo?.Category ?? candidate?.SourceCategory ?? string.Empty,
                ["ConceptTier"] = homeo?.ConceptTier ?? string.Empty,
            }),
            BuildStep(RubricEvidenceChainStepKeys.EmbeddingMatch, "Embedding Match", embeddingValue, candidate?.SimilarityScore, new Dictionary<string, string>
            {
                ["ConceptSimilarity"] = candidate?.SimilarityScore.ToString("0.0000") ?? string.Empty,
                ["ClinicalRelevance"] = candidate?.ClinicalRelevanceScore.ToString("0.0000") ?? string.Empty,
                ["EvidenceScore"] = candidate?.EvidenceScore.ToString("0.0000") ?? string.Empty,
                ["DoctorAcceptance"] = candidate?.DoctorAcceptanceScore.ToString("0.0000") ?? string.Empty,
                ["MatchMethod"] = candidate?.MatchMethod ?? rubric.MatchLayer ?? string.Empty,
            }),
            BuildStep(RubricEvidenceChainStepKeys.Rubric, "Rubric", rubric.SubSectionName, null, new Dictionary<string, string>
            {
                ["SubSectionId"] = rubric.SubSectionId.ToString(),
                ["RubricTier"] = rubric.RubricTier ?? string.Empty,
                ["ResultKind"] = rubric.ResultKind ?? string.Empty,
                ["SelectionReason"] = rubric.SelectionReason ?? rubric.WhySuggested ?? string.Empty,
                ["RepertoryPath"] = rubric.RepertoryPath ?? string.Empty,
                ["EnterpriseConfidence"] = (rubric.EnterpriseConfidenceScore ?? 0m).ToString("0.00"),
            }),
            BuildStep("WhySelected", "Why Selected", rubric.SelectionReason ?? rubric.WhySuggested ?? string.Empty, rubric.EnterpriseConfidenceScore),
            BuildStep("KnowledgeGraph", "Knowledge Graph", ResolveKgNode(context, rubric), null, new Dictionary<string, string>
            {
                ["PathCount"] = (context.Graph?.KnowledgeGraphPaths?.Count ?? 0).ToString(),
            }),
            BuildStep(RubricEvidenceChainStepKeys.Confidence, "Confidence", confidenceValue.ToString("0.00"), confidenceValue, new Dictionary<string, string>
            {
                ["QualityScore"] = (rubric.QualityScore ?? 0m).ToString("0.00"),
                ["CompositeScore"] = (candidate?.CompositeScore ?? confidenceValue).ToString("0.0000"),
            }),
            BuildStep(RubricEvidenceChainStepKeys.ValidationResult, "Validation Result", validationValue, validation?.Issues.Count == 0 ? 1m : 0m, BuildValidationMetadata(validation, rubric)),
            BuildStep(RubricEvidenceChainStepKeys.DoctorFeedback, "Doctor Feedback", FormatFeedback(feedback), feedback?.ConfidenceAtFeedback, BuildFeedbackMetadata(feedback)),
        };

        MarkPartialSteps(steps);

        return new RubricEnterpriseEvidenceChainModel
        {
            EngineVersion = rubric.EngineVersion ?? graph?.EngineVersion ?? "v8",
            SubSectionId = rubric.SubSectionId,
            RubricName = rubric.SubSectionName,
            Steps = steps,
            IsComplete = steps.All(s => s.Status == "Complete"),
        };
    }

    private static (HomeopathicConceptNodeModel? Homeo, ClinicalConceptNodeModel? Clinical, PatientMeaningNodeModel? Meaning)
        ResolveGraphNodes(
            AudioCaseSuggestedRubricModel rubric,
            ConceptGraphFullModel? graph,
            RubricDiscoveryNodeModel? discovery,
            RubricCandidateModel? candidate,
            IReadOnlyList<ClinicalConceptModel> legacyConcepts)
    {
        if (graph == null)
        {
            var legacy = legacyConcepts.FirstOrDefault(c =>
                c.ConceptId == rubric.SourceConceptId
                || string.Equals(c.HomeopathicMeaning, rubric.MatchedFrom, StringComparison.OrdinalIgnoreCase));

            if (legacy == null)
                return (null, null, null);

            return (
                new HomeopathicConceptNodeModel { ConceptName = legacy.HomeopathicMeaning ?? legacy.ClinicalMeaning ?? string.Empty, Confidence = legacy.Confidence },
                new ClinicalConceptNodeModel { ConceptName = legacy.ClinicalMeaning ?? legacy.RawStatement, Confidence = legacy.Confidence },
                new PatientMeaningNodeModel { RawStatement = legacy.RawStatement, NormalizedMeaning = legacy.RawStatement, Confidence = legacy.Confidence });
        }

        if (discovery != null)
        {
            var strict = ConceptGraphEvidenceEngine.ResolveStrictChain(discovery, graph);
            if (strict.Homeo != null)
                return strict;
        }

        var homeoId = discovery?.HomeopathicConceptId ?? candidate?.HomeopathicConceptId;
        var homeo = homeoId is > 0
            ? graph.HomeopathicConcepts.FirstOrDefault(h => h.HomeopathicConceptId == homeoId)
            : null;

        homeo ??= graph.HomeopathicConcepts.FirstOrDefault(h => h.HomeopathicConceptId == candidate?.HomeopathicConceptId)
            ?? graph.HomeopathicConcepts.FirstOrDefault(h =>
                string.Equals(h.ConceptName, candidate?.SourceConceptName, StringComparison.OrdinalIgnoreCase));

        if (homeo == null && candidate != null)
        {
            homeo = graph.HomeopathicConcepts.FirstOrDefault(h =>
                h.ClinicalConceptIndex == candidate.ClinicalConceptIndex);
        }

        var clinical = homeo != null && homeo.ClinicalConceptIndex >= 0
            ? graph.ClinicalConcepts.ElementAtOrDefault(homeo.ClinicalConceptIndex)
            : graph.ClinicalConcepts.FirstOrDefault(c => c.ClinicalConceptId == homeo?.ClinicalConceptId);

        var meaning = clinical != null && clinical.MeaningIndex >= 0
            ? graph.Meanings.ElementAtOrDefault(clinical.MeaningIndex)
            : graph.Meanings.FirstOrDefault(m => m.PatientMeaningId == clinical?.PatientMeaningId);

        return (homeo, clinical, meaning);
    }

    /// <summary>
    /// Prefer discovery owned by this rubric's concept (MatchedFrom / HomeopathicConceptId),
    /// never the first SubSectionId hit from an unrelated concept.
    /// </summary>
    private static RubricDiscoveryNodeModel? ResolveDiscovery(
        AudioCaseSuggestedRubricModel rubric,
        ConceptGraphFullModel? graph)
    {
        if (graph?.Discoveries == null || graph.Discoveries.Count == 0)
            return null;

        var byId = graph.Discoveries.Where(d => d.SubSectionId == rubric.SubSectionId).ToList();
        if (byId.Count == 0)
            return null;
        if (byId.Count == 1)
            return byId[0];

        if (!string.IsNullOrWhiteSpace(rubric.MatchedFrom))
        {
            var matched = byId.FirstOrDefault(d =>
                string.Equals(d.EvidenceChain?.TranscriptStatement, rubric.MatchedFrom, StringComparison.OrdinalIgnoreCase)
                || string.Equals(d.EvidenceChain?.PatientMeaning, rubric.MatchedFrom, StringComparison.OrdinalIgnoreCase)
                || string.Equals(d.EvidenceChain?.HomeopathicConcept, rubric.MatchedFrom, StringComparison.OrdinalIgnoreCase)
                || (d.MatchReason?.Contains(rubric.MatchedFrom, StringComparison.OrdinalIgnoreCase) ?? false));
            if (matched != null)
                return matched;
        }

        // Prefer discovery whose homeopathic concept name appears in MatchedFrom / WhySuggested.
        var byConceptName = byId.FirstOrDefault(d =>
        {
            var homeo = graph.HomeopathicConcepts.FirstOrDefault(h => h.HomeopathicConceptId == d.HomeopathicConceptId);
            if (homeo == null || string.IsNullOrWhiteSpace(homeo.ConceptName))
                return false;
            return (!string.IsNullOrWhiteSpace(rubric.MatchedFrom)
                    && rubric.MatchedFrom.Contains(homeo.ConceptName, StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrWhiteSpace(rubric.WhySuggested)
                    && rubric.WhySuggested.Contains(homeo.ConceptName, StringComparison.OrdinalIgnoreCase));
        });

        return byConceptName ?? byId.OrderByDescending(d => d.Confidence).First();
    }

    private static RubricCandidateModel? ResolveCandidate(
        AudioCaseSuggestedRubricModel rubric,
        RubricEvidenceChainEnrichmentContext context)
    {
        var byId = context.Candidates.Where(c => c.SubSectionId == rubric.SubSectionId).ToList();
        if (byId.Count == 0)
            return null;
        if (byId.Count == 1)
            return byId[0];

        if (!string.IsNullOrWhiteSpace(rubric.MatchedFrom))
        {
            var matched = byId.FirstOrDefault(c =>
                string.Equals(c.SourceConceptName, rubric.MatchedFrom, StringComparison.OrdinalIgnoreCase)
                || string.Equals(c.MappedFromConceptKey, rubric.MatchedFrom, StringComparison.OrdinalIgnoreCase)
                || (c.MatchReason?.Contains(rubric.MatchedFrom, StringComparison.OrdinalIgnoreCase) ?? false));
            if (matched != null)
                return matched;
        }

        return byId.OrderByDescending(c => c.CompositeScore).First();
    }

    private static RubricEvidenceChainStepModel BuildStep(
        string stepKey,
        string label,
        string? value,
        decimal? score = null,
        Dictionary<string, string>? metadata = null) =>
        new()
        {
            StepKey = stepKey,
            Label = label,
            Value = string.IsNullOrWhiteSpace(value) ? null : value.Trim(),
            Score = score,
            Status = string.IsNullOrWhiteSpace(value) ? "Missing" : "Complete",
            Metadata = metadata ?? new Dictionary<string, string>(),
        };

    private static Dictionary<string, string> BuildValidationMetadata(
        RubricEnterpriseValidationReport? validation,
        AudioCaseSuggestedRubricModel rubric)
    {
        var metadata = new Dictionary<string, string>
        {
            ["ValidationStatus"] = rubric.ValidationStatus ?? "Pending",
            ["QualityScore"] = (rubric.QualityScore ?? 0m).ToString("0.00"),
        };

        if (validation == null)
            return metadata;

        metadata["PassedAllSteps"] = validation.PassedAllSteps.ToString();
        metadata["FailedSteps"] = string.Join(", ",
            validation.Steps.Where(s => !s.Passed).Select(s => s.StepName));
        metadata["IssueCodes"] = string.Join(", ", validation.Issues.Select(i => i.Code).Distinct());
        return metadata;
    }

    private static Dictionary<string, string> BuildFeedbackMetadata(AudioCaseRubricFeedbackSummaryModel? feedback)
    {
        if (feedback == null)
            return new Dictionary<string, string> { ["Status"] = "Pending" };

        return new Dictionary<string, string>
        {
            ["FeedbackType"] = feedback.FeedbackType,
            ["EnteredDate"] = feedback.EnteredDate.ToString("O"),
            ["CorrectedSubSectionId"] = feedback.CorrectedSubSectionId?.ToString() ?? string.Empty,
        };
    }

    private static string? FormatFeedback(AudioCaseRubricFeedbackSummaryModel? feedback)
    {
        if (feedback == null)
            return "Pending doctor review";

        var reason = string.IsNullOrWhiteSpace(feedback.Reason) ? string.Empty : $" — {feedback.Reason}";
        return $"{feedback.FeedbackType}{reason}";
    }

    private static void MarkPartialSteps(IList<RubricEvidenceChainStepModel> steps)
    {
        foreach (var step in steps)
        {
            if (step.Status == "Missing"
                && step.StepKey is RubricEvidenceChainStepKeys.DoctorFeedback or RubricEvidenceChainStepKeys.EmbeddingMatch)
            {
                step.Status = "Partial";
                step.Value ??= "Not available";
            }
        }
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    private static string? ExtractTranscriptExcerpt(string transcript, string? needle)
    {
        if (string.IsNullOrWhiteSpace(transcript) || string.IsNullOrWhiteSpace(needle))
            return null;

        var index = transcript.IndexOf(needle.Split(' ').FirstOrDefault() ?? needle, StringComparison.OrdinalIgnoreCase);
        if (index < 0) return null;

        var start = Math.Max(0, index - 40);
        var length = Math.Min(transcript.Length - start, Math.Min(needle.Length + 80, 180));
        return transcript.Substring(start, length).Trim();
    }

    private static string? ResolveKgNode(RubricEvidenceChainEnrichmentContext context, AudioCaseSuggestedRubricModel rubric)
    {
        var paths = context.Graph?.KnowledgeGraphPaths;
        if (paths == null || paths.Count == 0)
        {
            return null;
        }

        var match = paths.FirstOrDefault(p =>
            p.SubSectionId == rubric.SubSectionId
            || string.Equals(p.SubSectionName, rubric.SubSectionName, StringComparison.OrdinalIgnoreCase));

        if (match == null)
        {
            return null;
        }

        if (match.Steps.Count == 0)
        {
            return match.SubSectionName;
        }

        return string.Join(" → ", match.Steps.Select(s => s.DisplayText).Where(l => !string.IsNullOrWhiteSpace(l)));
    }
}

public class EnterpriseRubricEvidenceChainEnricher : IRubricEnterpriseEvidenceChainEnricher
{
    private readonly IRubricEnterpriseEvidenceChainBuilder _builder;
    private readonly IRubricEvidenceChainFeedbackLoader _feedbackLoader;

    public EnterpriseRubricEvidenceChainEnricher(
        IRubricEnterpriseEvidenceChainBuilder builder,
        IRubricEvidenceChainFeedbackLoader feedbackLoader)
    {
        _builder = builder;
        _feedbackLoader = feedbackLoader;
    }

    public async Task EnrichRubricsAsync(
        IList<AudioCaseSuggestedRubricModel> rubrics,
        RubricEvidenceChainEnrichmentContext context,
        CancellationToken cancellationToken = default)
    {
        if (rubrics.Count == 0) return;

        var feedback = context.SessionId.HasValue
            ? await _feedbackLoader.LoadLatestBySubSectionAsync(context.SessionId.Value, cancellationToken)
            : context.DoctorFeedbackBySubSectionId;

        var enrichedContext = new RubricEvidenceChainEnrichmentContext
        {
            SessionId = context.SessionId,
            Transcript = context.Transcript,
            Graph = context.Graph,
            Candidates = context.Candidates,
            ValidationReports = context.ValidationReports,
            Concepts = context.Concepts,
            DoctorFeedbackBySubSectionId = feedback,
        };

        foreach (var rubric in rubrics)
        {
            rubric.EnterpriseEvidenceChain = _builder.Build(rubric, enrichedContext);
            if (rubric.Explainability != null)
                rubric.Explainability.EvidenceChain = rubric.EvidenceChain;
        }
    }

    public RubricEvidenceChainSessionResult BuildSessionResult(
        Guid sessionId,
        IEnumerable<AudioCaseSuggestedRubricModel> rubrics) =>
        new()
        {
            SessionId = sessionId,
            Chains = rubrics
                .Where(r => r.EnterpriseEvidenceChain != null)
                .Select(r => r.EnterpriseEvidenceChain!)
                .ToList(),
        };
}

public class RubricEvidenceChainFeedbackLoader : IRubricEvidenceChainFeedbackLoader
{
    private readonly Homeocentrum.Niga.NewAPI.Domain.Data.NIGACentrumContext _context;

    public RubricEvidenceChainFeedbackLoader(Homeocentrum.Niga.NewAPI.Domain.Data.NIGACentrumContext context) => _context = context;

    public async Task<IReadOnlyDictionary<int, AudioCaseRubricFeedbackSummaryModel>> LoadLatestBySubSectionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _context.AudioCaseRubricFeedbacks
            .AsNoTracking()
            .Where(x => x.AudioCaseSessionId == sessionId && x.SubSectionId.HasValue)
            .OrderByDescending(x => x.EnteredDate)
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(x => x.SubSectionId!.Value)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var latest = g.First();
                    return new AudioCaseRubricFeedbackSummaryModel
                    {
                        FeedbackType = latest.FeedbackType,
                        Reason = latest.Reason,
                        ConfidenceAtFeedback = latest.ConfidenceAtFeedback,
                        EnteredDate = latest.EnteredDate,
                        CorrectedSubSectionId = latest.CorrectedSubSectionId,
                    };
                });
    }
}
